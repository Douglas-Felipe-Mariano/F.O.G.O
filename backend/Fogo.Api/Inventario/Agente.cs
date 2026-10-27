namespace Fogo.Api.Inventario;

public enum SituacaoAgente
{
    Disponivel = 1,
    EmAtendimento = 2,
    Indisponivel = 3,
    Inativo = 4
}

public sealed class Agente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Nome { get; set; }
    public required string CategoriaProfissional { get; set; }
    public required string BaseOperacional { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public SituacaoAgente Situacao { get; set; }
    public OrigemRegistro OrigemDados { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}

public sealed class HistoricoLocalizacaoAgente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgenteId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTimeOffset RegistradaEm { get; set; }
}

public sealed record AgenteRequisicao(
    string Nome,
    string CategoriaProfissional,
    string BaseOperacional,
    decimal? Latitude,
    decimal? Longitude,
    SituacaoAgente Situacao,
    OrigemRegistro OrigemDados);

public sealed record AgenteResposta(
    Guid Id,
    string Nome,
    string CategoriaProfissional,
    string BaseOperacional,
    decimal? Latitude,
    decimal? Longitude,
    SituacaoAgente Situacao,
    OrigemRegistro OrigemDados,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm)
{
    public static AgenteResposta De(Agente agente) => new(
        agente.Id,
        agente.Nome,
        agente.CategoriaProfissional,
        agente.BaseOperacional,
        agente.Latitude,
        agente.Longitude,
        agente.Situacao,
        agente.OrigemDados,
        agente.CriadoEm,
        agente.AtualizadoEm);
}

public sealed record ContagemSituacaoAgente(SituacaoAgente Situacao, int Quantidade);

public sealed record IndicadoresAgentesResposta(
    int TotalCadastrado,
    IReadOnlyCollection<ContagemSituacaoAgente> PorSituacao,
    int EmAtividadeDeclarada);

public sealed record LocalizacaoAgenteResposta(
    Guid Id,
    string Nome,
    string CategoriaProfissional,
    string BaseOperacional,
    decimal Latitude,
    decimal Longitude,
    SituacaoAgente Situacao,
    OrigemRegistro OrigemDados = OrigemRegistro.Manual);

public sealed record HistoricoLocalizacaoAgenteResposta(
    Guid Id,
    Guid AgenteId,
    decimal Latitude,
    decimal Longitude,
    DateTimeOffset RegistradaEm);

public sealed record TrajetoAgenteResposta(
    Guid AgenteId,
    string NomeAgente,
    DateOnly Data,
    bool HistoricoDisponivel,
    string Mensagem,
    IReadOnlyCollection<HistoricoLocalizacaoAgenteResposta> Localizacoes);

public sealed record UltimasLocalizacoesAgenteResposta(
    Guid AgenteId,
    string NomeAgente,
    OrigemRegistro OrigemDados,
    bool HistoricoDisponivel,
    string Mensagem,
    IReadOnlyCollection<HistoricoLocalizacaoAgenteResposta> Localizacoes);

public sealed record HistoricoAgentePaginadoResposta(
    Guid AgenteId,
    string NomeAgente,
    OrigemRegistro OrigemDados,
    int Deslocamento,
    int Limite,
    int TotalRegistros,
    bool ResultadoTruncado,
    string Mensagem,
    IReadOnlyCollection<HistoricoLocalizacaoAgenteResposta> Localizacoes);

public sealed record MapaAgentesResposta(
    int TotalComCoordenadas,
    int Limite,
    bool ResultadoTruncado,
    IReadOnlyCollection<LocalizacaoAgenteResposta> Itens);

public sealed record PaginaAgentesResposta(
    int Pagina,
    int TamanhoPagina,
    int TotalRegistros,
    IReadOnlyCollection<AgenteResposta> Itens);
