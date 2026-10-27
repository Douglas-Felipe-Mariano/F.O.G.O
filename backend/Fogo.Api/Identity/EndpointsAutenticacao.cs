using System.Data;
using System.ComponentModel.DataAnnotations;
using Fogo.Api.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api;

public static class EndpointsAutenticacao
{
    public static IEndpointRouteBuilder MapearAutenticacao(this IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/autenticacao").WithTags("Autenticação");
        grupo.MapGet("/csrf", EmitirTokenCsrf)
            .AllowAnonymous()
            .WithSummary("Emite cookie de CSRF para iniciar a autenticação web.");
        grupo.MapPost("/entrar", EntrarAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .WithSummary("Autentica um usuário e cria uma sessão.");
        grupo.MapPost("/cadastro", CadastrarAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-publica")
            .WithSummary("Cria uma conta e envia um link para confirmação do e-mail.");
        grupo.MapPost("/confirmar-email", ConfirmarEmailAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-publica")
            .WithSummary("Confirma o endereço de e-mail usando um token temporário.");
        grupo.MapPost("/reenviar-confirmacao", ReenviarConfirmacaoAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-publica")
            .WithSummary("Solicita o reenvio da confirmação sem revelar se a conta existe.");
        grupo.MapPost("/esqueci-senha", EsqueciSenhaAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-publica")
            .WithSummary("Solicita um link de recuperação sem revelar se a conta existe.");
        grupo.MapPost("/redefinir-senha", RedefinirSenhaAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth-publica")
            .WithSummary("Redefine a senha com um token temporário enviado por e-mail.");
        grupo.MapPost("/atualizar", AtualizarAsync)
            .AllowAnonymous()
            .WithSummary("Rotaciona o refresh token da sessão.");
        grupo.MapPost("/sair", SairAsync)
            .AllowAnonymous()
            .WithSummary("Revoga a sessão atual.");

        return rotas;
    }

    private static IResult EmitirTokenCsrf(HttpContext contexto, OpcoesToken opcoes)
    {
        contexto.Response.Headers.CacheControl = "no-store";
        ProtecaoRequisicao.DefinirCookieCsrf(contexto, opcoes.CookiesSeguros);
        return Results.NoContent();
    }

    private static async Task<IResult> CadastrarAsync(
        CadastroRequisicao requisicao,
        HttpContext contexto,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        IEmailSenderFogo remetenteEmail,
        IConfiguration configuracao,
        IHostEnvironment ambiente,
        ILogger<Program> logger,
        IReadOnlyCollection<string> origensPermitidas,
        CancellationToken cancelamento)
    {
        if (!RequisicaoPublicaValida(contexto, origensPermitidas)) return Results.Forbid();
        if (!EmailValido(requisicao.Email))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Informe um endereço de e-mail válido."] });
        }

        if (string.IsNullOrEmpty(requisicao.Senha))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["senha"] = ["A senha é obrigatória."] });
        }

        var email = requisicao.Email.Trim();
        if (await gerenciadorUsuarios.FindByEmailAsync(email) is null)
        {
            var usuario = new UsuarioFogo { UserName = email, Email = email };
            var criacao = await gerenciadorUsuarios.CreateAsync(usuario, requisicao.Senha);
            if (criacao.Succeeded)
            {
                var token = await gerenciadorUsuarios.GenerateEmailConfirmationTokenAsync(usuario);
                await TentarEnviarLinkAsync(remetenteEmail, logger, configuracao, ambiente, email, token, "confirmar", cancelamento);
            }
            else if (criacao.Errors.Any(erro => erro.Code is "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric"))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["senha"] = criacao.Errors.Select(erro => erro.Description).ToArray() });
            }
        }

        return Results.Accepted(value: new { mensagem = "Se o cadastro puder ser concluído, enviaremos as instruções para o endereço informado." });
    }

    private static async Task<IResult> ReenviarConfirmacaoAsync(
        EmailRequisicao requisicao,
        HttpContext contexto,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        IEmailSenderFogo remetenteEmail,
        IConfiguration configuracao,
        IHostEnvironment ambiente,
        ILogger<Program> logger,
        IReadOnlyCollection<string> origensPermitidas,
        CancellationToken cancelamento)
    {
        if (!RequisicaoPublicaValida(contexto, origensPermitidas)) return Results.Forbid();
        if (EmailValido(requisicao.Email))
        {
            var usuario = await gerenciadorUsuarios.FindByEmailAsync(requisicao.Email.Trim());
            if (usuario is { EmailConfirmed: false })
            {
                var token = await gerenciadorUsuarios.GenerateEmailConfirmationTokenAsync(usuario);
                await TentarEnviarLinkAsync(remetenteEmail, logger, configuracao, ambiente, usuario.Email!, token, "confirmar", cancelamento);
            }
        }

        return Results.Accepted(value: new { mensagem = "Se houver uma conta pendente para o endereço informado, enviaremos novas instruções." });
    }

    private static async Task<IResult> ConfirmarEmailAsync(
        TokenEmailRequisicao requisicao,
        HttpContext contexto,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        IReadOnlyCollection<string> origensPermitidas)
    {
        if (!RequisicaoPublicaValida(contexto, origensPermitidas)) return Results.Forbid();
        var usuario = EmailValido(requisicao.Email)
            ? await gerenciadorUsuarios.FindByEmailAsync(requisicao.Email.Trim())
            : null;
        if (usuario is null || usuario.EmailConfirmed || string.IsNullOrWhiteSpace(requisicao.Token))
        {
            return Results.BadRequest(new { mensagem = "O link de confirmação é inválido ou expirou." });
        }

        var resultado = await gerenciadorUsuarios.ConfirmEmailAsync(usuario, requisicao.Token);
        return resultado.Succeeded
            ? Results.NoContent()
            : Results.BadRequest(new { mensagem = "O link de confirmação é inválido ou expirou. Solicite um novo." });
    }

    private static async Task<IResult> EsqueciSenhaAsync(
        EmailRequisicao requisicao,
        HttpContext contexto,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        IEmailSenderFogo remetenteEmail,
        IConfiguration configuracao,
        IHostEnvironment ambiente,
        ILogger<Program> logger,
        IReadOnlyCollection<string> origensPermitidas,
        CancellationToken cancelamento)
    {
        if (!RequisicaoPublicaValida(contexto, origensPermitidas)) return Results.Forbid();
        if (EmailValido(requisicao.Email))
        {
            var usuario = await gerenciadorUsuarios.FindByEmailAsync(requisicao.Email.Trim());
            if (usuario is not null)
            {
                var token = await gerenciadorUsuarios.GeneratePasswordResetTokenAsync(usuario);
                await TentarEnviarLinkAsync(remetenteEmail, logger, configuracao, ambiente, usuario.Email!, token, "redefinir", cancelamento);
            }
        }

        return Results.Accepted(value: new { mensagem = "Se houver uma conta associada ao endereço, enviaremos as instruções de recuperação." });
    }

    private static async Task<IResult> RedefinirSenhaAsync(
        RedefinirSenhaRequisicao requisicao,
        HttpContext contexto,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        FogoDbContext banco,
        TimeProvider relogio,
        IReadOnlyCollection<string> origensPermitidas,
        CancellationToken cancelamento)
    {
        if (!RequisicaoPublicaValida(contexto, origensPermitidas)) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(requisicao.Token) || string.IsNullOrEmpty(requisicao.NovaSenha))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["token"] = ["Informe o token e a nova senha."] });
        }

        var usuario = EmailValido(requisicao.Email)
            ? await gerenciadorUsuarios.FindByEmailAsync(requisicao.Email.Trim())
            : null;
        if (usuario is null)
        {
            return Results.BadRequest(new { mensagem = "O link de recuperação é inválido ou expirou." });
        }

        var resultado = await gerenciadorUsuarios.ResetPasswordAsync(usuario, requisicao.Token, requisicao.NovaSenha);
        if (!resultado.Succeeded)
        {
            if (resultado.Errors.Any(erro => erro.Code.StartsWith("Password", StringComparison.Ordinal)))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["novaSenha"] = resultado.Errors.Select(erro => erro.Description).ToArray() });
            }

            return Results.BadRequest(new { mensagem = "O link de recuperação é inválido ou expirou." });
        }

        var agora = relogio.GetUtcNow();
        await banco.SessoesAtualizacao
            .Where(sessao => sessao.UsuarioId == usuario.Id && sessao.RevogadaEm == null)
            .ExecuteUpdateAsync(atualizacao => atualizacao.SetProperty(sessao => sessao.RevogadaEm, agora), cancelamento);
        return Results.NoContent();
    }

    private static bool RequisicaoPublicaValida(HttpContext contexto, IReadOnlyCollection<string> origensPermitidas)
    {
        contexto.Response.Headers.CacheControl = "no-store";
        return ProtecaoRequisicao.OrigemPermitida(contexto, origensPermitidas)
            && ProtecaoRequisicao.TokenCsrfValido(contexto);
    }

    private static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Length <= 256
        && new EmailAddressAttribute().IsValid(email.Trim());

    private static async Task TentarEnviarLinkAsync(
        IEmailSenderFogo remetenteEmail,
        ILogger logger,
        IConfiguration configuracao,
        IHostEnvironment ambiente,
        string email,
        string token,
        string acao,
        CancellationToken cancelamento)
    {
        try
        {
            var link = LinksEmailAutenticacao.Criar(configuracao, ambiente, acao, email, token);
            var linkHtml = LinksEmailAutenticacao.HtmlSeguro(link);
            var texto = acao == "confirmar"
                ? $"Confirme seu e-mail acessando: {link}"
                : $"Redefina sua senha acessando: {link}";
            var html = $"<p>{(acao == "confirmar" ? "Confirme seu endereço de e-mail" : "Solicitação para redefinir sua senha")} no F.O.G.O.:</p><p><a href=\"{linkHtml}\">{(acao == "confirmar" ? "Confirmar e-mail" : "Redefinir senha")}</a></p><p>Se você não solicitou esta ação, ignore esta mensagem.</p>";
            var assunto = acao == "confirmar" ? "Confirme seu e-mail — F.O.G.O." : "Recuperação de senha — F.O.G.O.";
            await remetenteEmail.EnviarAsync(email, assunto, texto, html, cancelamento);
            logger.LogInformation("E-mail de autenticação aceito pelo servidor SMTP. Ação: {Acao}; destinatário: {DestinatarioMascarado}", acao, MascararEmail(email));
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            var statusSmtp = excecao is MailKit.Net.Smtp.SmtpCommandException falhaSmtp
                ? falhaSmtp.StatusCode.ToString()
                : "não disponível";
            logger.LogError("Não foi possível enviar e-mail de autenticação. Tipo da falha: {TipoFalha}; status SMTP: {StatusSmtp}", excecao.GetType().Name, statusSmtp);
        }
    }

    private static string MascararEmail(string email)
    {
        var separador = email.LastIndexOf('@');
        if (separador <= 0 || separador == email.Length - 1) return "endereço inválido";
        var local = email[..separador];
        var dominio = email[(separador + 1)..];
        return $"{local[0]}***@{dominio[0]}***";
    }

    private static async Task<IResult> EntrarAsync(
        LoginRequisicao requisicao,
        HttpContext contexto,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        SignInManager<UsuarioFogo> gerenciadorAcesso,
        FogoDbContext banco,
        ServicoTokens servicoTokens,
        TimeProvider relogio,
        OpcoesToken opcoes,
        IReadOnlyCollection<string> origensPermitidas,
        CancellationToken cancelamento)
    {
        if (!ProtecaoRequisicao.OrigemPermitida(contexto, origensPermitidas)
            || !ProtecaoRequisicao.TokenCsrfValido(contexto))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(requisicao.Identificador) || string.IsNullOrEmpty(requisicao.Senha))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["credenciais"] = ["Informe usuário e senha."] });
        }

        var usuario = await gerenciadorUsuarios.FindByNameAsync(requisicao.Identificador)
            ?? await gerenciadorUsuarios.FindByEmailAsync(requisicao.Identificador);
        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        var resultadoSenha = await gerenciadorAcesso.CheckPasswordSignInAsync(usuario, requisicao.Senha, lockoutOnFailure: true);
        if (!resultadoSenha.Succeeded)
        {
            return Results.Unauthorized();
        }

        var agora = relogio.GetUtcNow();
        var (tokenAtualizacao, hashToken) = servicoTokens.CriarTokenAtualizacao();
        banco.SessoesAtualizacao.Add(new SessaoAtualizacao
        {
            UsuarioId = usuario.Id,
            HashToken = hashToken,
            CriadaEm = agora,
            ExpiraEm = agora.Add(opcoes.DuracaoAtualizacao)
        });
        await banco.SaveChangesAsync(cancelamento);

        ProtecaoRequisicao.DefinirCookies(contexto, tokenAtualizacao, agora.Add(opcoes.DuracaoAtualizacao), opcoes.CookiesSeguros);
        var tokenAcesso = servicoTokens.CriarTokenAcesso(usuario);
        return Results.Ok(new RespostaTokenAcesso(tokenAcesso.Valor, tokenAcesso.ExpiraEm));
    }

    private static async Task<IResult> AtualizarAsync(
        HttpContext contexto,
        FogoDbContext banco,
        UserManager<UsuarioFogo> gerenciadorUsuarios,
        ServicoTokens servicoTokens,
        TimeProvider relogio,
        OpcoesToken opcoes,
        IReadOnlyCollection<string> origensPermitidas,
        CancellationToken cancelamento)
    {
        if (!ProtecaoRequisicao.OrigemPermitida(contexto, origensPermitidas)
            || !ProtecaoRequisicao.TokenCsrfValido(contexto))
        {
            return Results.Forbid();
        }

        var tokenAtualizacao = contexto.Request.Cookies[ProtecaoRequisicao.CookieAtualizacao];
        if (string.IsNullOrWhiteSpace(tokenAtualizacao))
        {
            ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
            return Results.Unauthorized();
        }

        await using var transacao = await banco.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelamento);
        var hashAtual = ServicoTokens.CalcularHash(tokenAtualizacao);
        var sessao = await banco.SessoesAtualizacao.SingleOrDefaultAsync(item => item.HashToken == hashAtual, cancelamento);
        if (sessao is null)
        {
            await transacao.CommitAsync(cancelamento);
            ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
            return Results.Unauthorized();
        }

        var agora = relogio.GetUtcNow();
        if (sessao.RevogadaEm is not null)
        {
            await RevogarSessoesUsuarioAsync(banco, sessao.UsuarioId, agora, cancelamento);
            await transacao.CommitAsync(cancelamento);
            ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
            return Results.Unauthorized();
        }

        if (sessao.ExpiraEm <= agora)
        {
            sessao.RevogadaEm = agora;
            await banco.SaveChangesAsync(cancelamento);
            await transacao.CommitAsync(cancelamento);
            ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
            return Results.Unauthorized();
        }

        var novoToken = servicoTokens.CriarTokenAtualizacao();
        var atualizado = await banco.SessoesAtualizacao
            .Where(item => item.Id == sessao.Id && item.RevogadaEm == null)
            .ExecuteUpdateAsync(atualizacao => atualizacao
                .SetProperty(item => item.RevogadaEm, agora)
                .SetProperty(item => item.SubstituidaPorHash, novoToken.Hash), cancelamento);

        if (atualizado != 1)
        {
            await RevogarSessoesUsuarioAsync(banco, sessao.UsuarioId, agora, cancelamento);
            await transacao.CommitAsync(cancelamento);
            ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
            return Results.Unauthorized();
        }

        var expiraEm = agora.Add(opcoes.DuracaoAtualizacao);
        banco.SessoesAtualizacao.Add(new SessaoAtualizacao
        {
            UsuarioId = sessao.UsuarioId,
            HashToken = novoToken.Hash,
            CriadaEm = agora,
            ExpiraEm = expiraEm
        });
        await banco.SaveChangesAsync(cancelamento);
        await transacao.CommitAsync(cancelamento);

        var usuario = await gerenciadorUsuarios.FindByIdAsync(sessao.UsuarioId);
        if (usuario is null)
        {
            ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
            return Results.Unauthorized();
        }

        ProtecaoRequisicao.DefinirCookies(contexto, novoToken.Token, expiraEm, opcoes.CookiesSeguros);
        var tokenAcesso = servicoTokens.CriarTokenAcesso(usuario);
        return Results.Ok(new RespostaTokenAcesso(tokenAcesso.Valor, tokenAcesso.ExpiraEm));
    }

    private static async Task<IResult> SairAsync(
        HttpContext contexto,
        FogoDbContext banco,
        TimeProvider relogio,
        IReadOnlyCollection<string> origensPermitidas,
        OpcoesToken opcoes,
        CancellationToken cancelamento)
    {
        if (!ProtecaoRequisicao.OrigemPermitida(contexto, origensPermitidas)
            || !ProtecaoRequisicao.TokenCsrfValido(contexto))
        {
            return Results.Forbid();
        }

        var token = contexto.Request.Cookies[ProtecaoRequisicao.CookieAtualizacao];
        if (!string.IsNullOrWhiteSpace(token))
        {
            var hash = ServicoTokens.CalcularHash(token);
            var sessao = await banco.SessoesAtualizacao.SingleOrDefaultAsync(item => item.HashToken == hash, cancelamento);
            if (sessao is { RevogadaEm: null })
            {
                sessao.RevogadaEm = relogio.GetUtcNow();
                await banco.SaveChangesAsync(cancelamento);
            }
        }

        ProtecaoRequisicao.LimparCookies(contexto, opcoes.CookiesSeguros);
        return Results.NoContent();
    }

    private static async Task RevogarSessoesUsuarioAsync(
        FogoDbContext banco,
        string usuarioId,
        DateTimeOffset agora,
        CancellationToken cancelamento)
    {
        await banco.SessoesAtualizacao
            .Where(sessao => sessao.UsuarioId == usuarioId && sessao.RevogadaEm == null)
            .ExecuteUpdateAsync(atualizacao => atualizacao.SetProperty(sessao => sessao.RevogadaEm, agora), cancelamento);
    }
}

public sealed record LoginRequisicao(string Identificador, string Senha);
public sealed record CadastroRequisicao(string Email, string Senha);
public sealed record EmailRequisicao(string Email);
public sealed record TokenEmailRequisicao(string Email, string Token);
public sealed record RedefinirSenhaRequisicao(string Email, string Token, string NovaSenha);
public sealed record RespostaTokenAcesso(string TokenAcesso, DateTimeOffset ExpiraEm);