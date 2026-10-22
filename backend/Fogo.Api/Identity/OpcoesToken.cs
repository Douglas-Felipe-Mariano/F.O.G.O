namespace Fogo.Api.Identity;

public sealed class OpcoesToken
{
    public const string Secao = "Autenticacao";

    public required string Emissor { get; init; }
    public required string Audiencia { get; init; }
    public required string ChaveAssinatura { get; init; }
    public TimeSpan DuracaoAcesso { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan DuracaoAtualizacao { get; init; } = TimeSpan.FromDays(7);
    public bool CookiesSeguros { get; init; } = true;
}