using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Fogo.Api.Identity;

public sealed class ServicoTokens(OpcoesToken opcoes, TimeProvider relogio)
{
    public TokenAcesso CriarTokenAcesso(UsuarioFogo usuario)
    {
        var agora = relogio.GetUtcNow();
        var credencial = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.ChaveAssinatura)),
            SecurityAlgorithms.HmacSha256);
        var identidade = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.UserName ?? string.Empty)
        ]);
        var jwt = new JwtSecurityToken(
            opcoes.Emissor,
            opcoes.Audiencia,
            identidade.Claims,
            agora.UtcDateTime,
            agora.Add(opcoes.DuracaoAcesso).UtcDateTime,
            credencial);

        return new TokenAcesso(new JwtSecurityTokenHandler().WriteToken(jwt), agora.Add(opcoes.DuracaoAcesso));
    }

    public (string Token, string Hash) CriarTokenAtualizacao()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (token, CalcularHash(token));
    }

    public static string CalcularHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed record TokenAcesso(string Valor, DateTimeOffset ExpiraEm);