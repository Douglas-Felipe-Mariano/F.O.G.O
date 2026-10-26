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
    Task<Equipamento?> ConsultarAsync(Guid id, CancellationToken cancelamento);
    Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento);
    Task<int> ContarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, CancellationToken cancelamento);
    Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, int deslocamento, int limite, CancellationToken cancelamento);
    Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento);
    Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento);
    Task AdicionarAsync(Equipamento equipamento, CancellationToken cancelamento);
    Task SalvarAlteracoesAsync(CancellationToken cancelamento);
}
