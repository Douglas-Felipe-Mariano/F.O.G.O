using System.Data;
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
public sealed record RespostaTokenAcesso(string TokenAcesso, DateTimeOffset ExpiraEm);