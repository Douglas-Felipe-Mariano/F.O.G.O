using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Fogo.Api.Identity;

public static class ProtecaoRequisicao
{
    public const string CookieAtualizacao = "fogo.atualizacao";
    public const string CookieCsrf = "fogo.csrf";
    public const string CabecalhoCsrf = "X-CSRF-TOKEN";

    public static bool OrigemPermitida(HttpContext contexto, IReadOnlyCollection<string> origensPermitidas)
    {
        var origem = contexto.Request.Headers.Origin.ToString();
        if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath is not ("/" or "")
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        var origemNormalizada = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return origensPermitidas.Any(origemPermitida =>
            Uri.TryCreate(origemPermitida, UriKind.Absolute, out var permitida)
            && string.IsNullOrEmpty(permitida.UserInfo)
            && permitida.AbsolutePath is "/" or ""
            && string.IsNullOrEmpty(permitida.Query)
            && string.IsNullOrEmpty(permitida.Fragment)
            && string.Equals(
                permitida.GetLeftPart(UriPartial.Authority).TrimEnd('/'),
                origemNormalizada,
                StringComparison.OrdinalIgnoreCase));
    }

    public static bool TokenCsrfValido(HttpContext contexto)
    {
        var cookie = contexto.Request.Cookies[CookieCsrf];
        var cabecalho = contexto.Request.Headers[CabecalhoCsrf].ToString();

        if (string.IsNullOrWhiteSpace(cookie) || string.IsNullOrWhiteSpace(cabecalho))
        {
            return false;
        }

        var bytesCookie = Encoding.UTF8.GetBytes(cookie);
        var bytesCabecalho = Encoding.UTF8.GetBytes(cabecalho);
        return bytesCookie.Length == bytesCabecalho.Length && CryptographicOperations.FixedTimeEquals(bytesCookie, bytesCabecalho);
    }

    public static void DefinirCookies(HttpContext contexto, string tokenAtualizacao, DateTimeOffset expiraEm, bool cookiesSeguros)
    {
        var tokenCsrf = CriarTokenCsrf();
        var opcoesAtualizacao = new CookieOptions
        {
            HttpOnly = true,
            Secure = cookiesSeguros,
            SameSite = SameSiteMode.Strict,
            Path = "/api/autenticacao/",
            Expires = expiraEm,
            IsEssential = true
        };
        var opcoesCsrf = new CookieOptions
        {
            HttpOnly = false,
            Secure = cookiesSeguros,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = opcoesAtualizacao.Expires,
            IsEssential = true
        };

        contexto.Response.Cookies.Append(CookieAtualizacao, tokenAtualizacao, opcoesAtualizacao);
        contexto.Response.Cookies.Append(CookieCsrf, tokenCsrf, opcoesCsrf);
    }

    public static void DefinirCookieCsrf(HttpContext contexto, bool cookiesSeguros)
    {
        var tokenCsrf = CriarTokenCsrf();
        contexto.Response.Cookies.Append(CookieCsrf, tokenCsrf, new CookieOptions
        {
            HttpOnly = false,
            Secure = cookiesSeguros,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        });
    }

    public static void LimparCookies(HttpContext contexto, bool cookiesSeguros)
    {
        var opcoes = new CookieOptions
        {
            HttpOnly = true,
            Secure = cookiesSeguros,
            SameSite = SameSiteMode.Strict,
            Path = "/api/autenticacao/",
            IsEssential = true
        };

        contexto.Response.Cookies.Delete(CookieAtualizacao, opcoes);
        contexto.Response.Cookies.Delete(CookieCsrf, new CookieOptions
        {
            Secure = cookiesSeguros,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        });
    }

    private static string CriarTokenCsrf() =>
        Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}