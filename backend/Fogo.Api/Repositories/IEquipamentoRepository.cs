using Fogo.Api.Inventario;

namespace Fogo.Api.Repositories;

public sealed record FiltrosEquipamentos(
    int Pagina,
    int TamanhoPagina,
    string? Tipo,
    SituacaoEquipamento? Situacao,
    string? Bairro,
    bool? NecessitaManutencao = null);

public sealed record FiltrosMapaEquipamentos(
    string? Tipo,
    SituacaoEquipamento? Situacao,
    string? Bairro);

public interface IEquipamentoRepository
{
    Task<int> ContarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento);
    Task<IReadOnlyCollection<Equipamento>> ListarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento);
    Task<IReadOnlyCollection<string>> ListarTiposAsync(CancellationToken cancelamento);
    Task<Equipamento?> ConsultarAsync(Guid id, CancellationToken cancelamento);
    Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento);
    Task<int> ContarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, CancellationToken cancelamento);
    Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, int deslocamento, int limite, CancellationToken cancelamento);
    Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento);
    Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento);
    Task<int> ContarManutencoesImediatasAsync(CancellationToken cancelamento);
    Task<int> ContarManutencoesComDataAteAsync(DateOnly limite, CancellationToken cancelamento);
    Task<int> ContarManutencoesEntreAsync(DateOnly inicio, DateOnly fim, CancellationToken cancelamento);
    Task<IReadOnlyCollection<ResumoOperacionalLocalidade>> ConsultarResumoPorLocalidadeAsync(
        DateOnly hoje,
        DateOnly fimJanelaAviso,
        CancellationToken cancelamento);
    Task<IReadOnlyCollection<ProjecaoManutencaoLocalidade>> ConsultarProjecaoManutencaoAsync(
        DateOnly hoje,
        DateOnly fimHorizonte,
        CancellationToken cancelamento);
    Task AdicionarAsync(Equipamento equipamento, CancellationToken cancelamento);
    Task SalvarAlteracoesAsync(CancellationToken cancelamento);
}
