using Fogo.Api.Inventario;

namespace Fogo.Api.Operacoes;

public sealed class SegmentoTransito
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Cidade { get; set; }
    public required string ReferenciaMapa { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public OrigemRegistro OrigemDados { get; set; }
}

public sealed class ObservacaoTransito
{
    public Guid Id { get; set; }
    public Guid SegmentoId { get; set; }
    public DateTimeOffset RegistradaEm { get; set; }
    public int VeiculosUltimaHora { get; set; }
    public decimal VelocidadeMediaKmh { get; set; }
    public int CongestionamentoPercentual { get; set; }
    public OrigemRegistro OrigemDados { get; set; }
}

public enum GrauAcidente
{
    Leve = 1,
    Moderado = 2,
    Grave = 3,
    Critico = 4
}

public enum ImpactoTransito
{
    Nenhum = 1,
    Lentidao = 2,
    BloqueioParcial = 3,
    InterdicaoTotal = 4
}

public enum SituacaoAcidente
{
    Aberto = 1,
    AtendimentoSolicitado = 2,
    Encerrado = 3
}

public sealed class OcorrenciaAcidente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string LocalDescricao { get; set; }
    public required string Cidade { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public GrauAcidente Grau { get; set; }
    public ImpactoTransito ImpactoTransito { get; set; }
    public bool HaFeridos { get; set; }
    public SituacaoAcidente Situacao { get; set; } = SituacaoAcidente.Aberto;
    public DateTimeOffset RegistradoEm { get; set; }
    public DateTimeOffset? DespachoSolicitadoEm { get; set; }
    public Guid? AgenteDesignadoId { get; set; }
    public DateTimeOffset? SocorroSolicitadoEm { get; set; }
}

public sealed record AcidenteRequisicao(
    string LocalDescricao,
    string Cidade,
    decimal Latitude,
    decimal Longitude,
    GrauAcidente Grau,
    ImpactoTransito ImpactoTransito,
    bool HaFeridos);

public sealed record DespachoAgenteRequisicao(Guid? AgenteId);

public sealed record AcidenteResposta(
    Guid Id,
    string LocalDescricao,
    string Cidade,
    decimal Latitude,
    decimal Longitude,
    GrauAcidente Grau,
    ImpactoTransito ImpactoTransito,
    bool HaFeridos,
    SituacaoAcidente Situacao,
    DateTimeOffset RegistradoEm,
    DateTimeOffset? DespachoSolicitadoEm,
    Guid? AgenteDesignadoId,
    DateTimeOffset? SocorroSolicitadoEm)
{
    public static AcidenteResposta De(OcorrenciaAcidente acidente) => new(
        acidente.Id,
        acidente.LocalDescricao,
        acidente.Cidade,
        acidente.Latitude,
        acidente.Longitude,
        acidente.Grau,
        acidente.ImpactoTransito,
        acidente.HaFeridos,
        acidente.Situacao,
        acidente.RegistradoEm,
        acidente.DespachoSolicitadoEm,
        acidente.AgenteDesignadoId,
        acidente.SocorroSolicitadoEm);
}

public sealed record TransitoSegmentoResposta(
    Guid Id,
    string Nome,
    string Cidade,
    string ReferenciaMapa,
    decimal Latitude,
    decimal Longitude,
    int VeiculosUltimaHora,
    decimal VelocidadeMediaKmh,
    int CongestionamentoPercentual,
    DateTimeOffset AmostraEm,
    OrigemRegistro OrigemDados,
    int VeiculosPrevistosProximaHora,
    decimal VelocidadePrevistaKmh,
    int CongestionamentoPrevistoPercentual,
    int AmostrasHistoricas,
    string Confianca,
    DateTimeOffset HorizontePrevisto);

public sealed record AlertaOperacionalResposta(
    string Tipo,
    string Severidade,
    string Titulo,
    string Descricao,
    Guid? RecursoId,
    string Cidade,
    decimal? Latitude,
    decimal? Longitude,
    OrigemRegistro OrigemDados,
    DateTimeOffset GeradoEm);
