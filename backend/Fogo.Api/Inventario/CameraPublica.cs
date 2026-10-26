namespace Fogo.Api.Inventario;

public sealed class CameraPublica
{
    public Guid Id { get; set; }
    public required string Cidade { get; set; }
    public required string Nome { get; set; }
    public required string UrlStream { get; set; }
    public string? Endereco { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public sealed record CameraPublicaResposta(
    Guid Id,
    string Cidade,
    string Nome,
    string? Endereco,
    decimal? Latitude,
    decimal? Longitude,
    string UrlStream);

public sealed record PaginaCamerasResposta(
    int Total,
    IReadOnlyCollection<CameraPublicaResposta> Itens);