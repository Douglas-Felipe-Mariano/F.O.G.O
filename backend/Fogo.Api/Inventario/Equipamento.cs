namespace Fogo.Api.Inventario;

public enum SituacaoEquipamento
{
    NaoInformado = 0,
    AindaNaoInstalado = 1,
    Operacional = 2,
    MauFuncionamento = 3,
    EmManutencao = 4,
    Desativado = 5,
    Furtado = 6
}

public enum TipoManutencao
{
    Imediata = 1,
    Agendada = 2,
    Periodica = 3
}

public enum OrigemRegistro
{
    NaoInformada = 0,
    Manual = 1,
    Importado = 2,
    Simulado = 3
}

public sealed class Equipamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Tipo { get; set; }
    public required string NumeroSerie { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public required string Cep { get; set; }
    public required string Bairro { get; set; }
    public required string Cidade { get; set; }
    public required string Uf { get; set; }
    public required string Rua { get; set; }
    public string? Referencia { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public SituacaoEquipamento Situacao { get; set; }
    public bool NecessitaManutencao { get; set; }
    public TipoManutencao? TipoManutencao { get; set; }
    public DateOnly? ProximaManutencao { get; set; }
    public OrigemRegistro OrigemDados { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}

public sealed record EquipamentoRequisicao(
    string Tipo,
    string NumeroSerie,
    string? Marca,
    string? Modelo,
    string Cep,
    string Bairro,
    string Cidade,
    string Uf,
    string Rua,
    string? Referencia,
    decimal? Latitude,
    decimal? Longitude,
    SituacaoEquipamento Situacao,
    bool NecessitaManutencao,
    TipoManutencao? TipoManutencao,
    DateOnly? ProximaManutencao,
    OrigemRegistro OrigemDados);

public sealed record EquipamentoResposta(
    Guid Id,
    string Tipo,
    string NumeroSerie,
    string? Marca,
    string? Modelo,
    string Cep,
    string Bairro,
    string Cidade,
    string Uf,
    string Rua,
    string? Referencia,
    decimal? Latitude,
    decimal? Longitude,
    SituacaoEquipamento Situacao,
    bool NecessitaManutencao,
    TipoManutencao? TipoManutencao,
    DateOnly? ProximaManutencao,
    OrigemRegistro OrigemDados,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm)
{
    public static EquipamentoResposta De(Equipamento equipamento) => new(
        equipamento.Id,
        equipamento.Tipo,
        equipamento.NumeroSerie,
        equipamento.Marca,
        equipamento.Modelo,
        equipamento.Cep,
        equipamento.Bairro,
        equipamento.Cidade,
        equipamento.Uf,
        equipamento.Rua,
        equipamento.Referencia,
        equipamento.Latitude,
        equipamento.Longitude,
        equipamento.Situacao,
        equipamento.NecessitaManutencao,
        equipamento.TipoManutencao,
        equipamento.ProximaManutencao,
        equipamento.OrigemDados,
        equipamento.CriadoEm,
        equipamento.AtualizadoEm);
}

public sealed record PaginaEquipamentosResposta(
    int Pagina,
    int TamanhoPagina,
    int TotalRegistros,
    IReadOnlyCollection<EquipamentoResposta> Itens);

public sealed record ContagemSituacao(SituacaoEquipamento Situacao, int Quantidade);

public sealed record LocalizacaoEquipamentoResposta(
    Guid Id,
    string Tipo,
    string NumeroSerie,
    string Bairro,
    string Cidade,
    string Uf,
    string Rua,
    string? Referencia,
    decimal Latitude,
    decimal Longitude,
    SituacaoEquipamento Situacao,
    OrigemRegistro OrigemDados = OrigemRegistro.Manual);

public sealed record MapaEquipamentosResposta(
    int TotalComCoordenadas,
    int Limite,
    bool ResultadoTruncado,
    IReadOnlyCollection<LocalizacaoEquipamentoResposta> Itens);