namespace Fogo.Api.Identity;

public sealed class SessaoAtualizacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UsuarioId { get; set; }
    public required string HashToken { get; set; }
    public DateTimeOffset CriadaEm { get; set; }
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? RevogadaEm { get; set; }
    public string? SubstituidaPorHash { get; set; }
}