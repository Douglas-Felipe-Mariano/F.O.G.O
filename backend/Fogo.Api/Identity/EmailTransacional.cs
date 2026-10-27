using System.Text.Encodings.Web;
using System.ComponentModel.DataAnnotations;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Fogo.Api.Identity;

public interface IEmailSenderFogo
{
    Task EnviarAsync(string destinatario, string assunto, string texto, string html, CancellationToken cancelamento);
}

public sealed class SmtpEmailSender(IConfiguration configuracao) : IEmailSenderFogo
{
    public async Task EnviarAsync(string destinatario, string assunto, string texto, string html, CancellationToken cancelamento)
    {
        var host = configuracao["FOGO_SMTP_HOST"];
        var remetente = configuracao["FOGO_SMTP_FROM"];
        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(remetente)
            || destinatario.Any(char.IsControl)
            || !new EmailAddressAttribute().IsValid(destinatario)
            || remetente.Any(char.IsControl)
            || !new EmailAddressAttribute().IsValid(remetente))
        {
            throw new InvalidOperationException("Configure um host SMTP e endereços de e-mail válidos em FOGO_SMTP_FROM e na solicitação.");
        }

        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(configuracao["FOGO_SMTP_FROM_NAME"] ?? "F.O.G.O.", remetente));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = assunto;
        mensagem.Body = new BodyBuilder { TextBody = texto, HtmlBody = html }.ToMessageBody();

        var porta = configuracao.GetValue("FOGO_SMTP_PORT", 587);
        var modoSeguranca = configuracao["FOGO_SMTP_SECURITY"]?.Trim().ToLowerInvariant();
        var usuario = configuracao["FOGO_SMTP_USER"];
        var senha = configuracao["FOGO_SMTP_PASSWORD"];
        if (string.IsNullOrWhiteSpace(usuario) != string.IsNullOrEmpty(senha))
        {
            throw new InvalidOperationException("Configure FOGO_SMTP_USER e FOGO_SMTP_PASSWORD em conjunto.");
        }
        var seguranca = modoSeguranca switch
        {
            "ssl" or "sslonconnect" => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.StartTls
        };
        if (modoSeguranca == "none")
        {
            var hostLocal = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || System.Net.IPAddress.TryParse(host, out var endereco) && System.Net.IPAddress.IsLoopback(endereco);
            if (!hostLocal || !string.IsNullOrWhiteSpace(usuario) || !string.IsNullOrEmpty(senha))
            {
                throw new InvalidOperationException("SMTP sem TLS só é permitido para host local sem autenticação.");
            }

            seguranca = SecureSocketOptions.None;
        }

        using var cliente = new SmtpClient();
        cliente.Timeout = 10_000;
        await cliente.ConnectAsync(host, porta, seguranca, cancelamento);
        if (!string.IsNullOrWhiteSpace(usuario))
        {
            if (string.IsNullOrEmpty(senha))
            {
                throw new InvalidOperationException("FOGO_SMTP_PASSWORD é obrigatório quando FOGO_SMTP_USER está configurado.");
            }

            await cliente.AuthenticateAsync(usuario, senha, cancelamento);
        }

        await cliente.SendAsync(mensagem, cancelamento);
        await cliente.DisconnectAsync(true, cancelamento);
    }
}

public static class LinksEmailAutenticacao
{
    public static string Criar(IConfiguration configuracao, IHostEnvironment ambiente, string acao, string email, string token)
    {
        var endereco = configuracao["FOGO_PUBLIC_URL"];
        if (string.IsNullOrWhiteSpace(endereco) && ambiente.IsDevelopment())
        {
            endereco = "http://localhost:8080";
        }

        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("Configure FOGO_PUBLIC_URL como a origem HTTPS pública da aplicação (HTTP somente em loopback local).");
        }

        var consulta = string.Join("&", new[]
        {
            $"acao={Uri.EscapeDataString(acao)}",
            $"email={Uri.EscapeDataString(email)}",
            $"token={Uri.EscapeDataString(token)}"
        });
        return new UriBuilder(uri) { Query = consulta }.Uri.ToString();
    }

    public static string HtmlSeguro(string valor) => HtmlEncoder.Default.Encode(valor);
}