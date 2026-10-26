using Fogo.Api.Inventario;

namespace Fogo.Api.Repositories;

public sealed record FiltrosAgentes(
    int Pagina,
    int TamanhoPagina,
    string? CategoriaProfissional,
    SituacaoAgente? Situacao);

public interface IAgenteRepository
{
    Task<int> ContarAsync(FiltrosAgentes filtros, CancellationToken cancelamento);
    Task<IReadOnlyCollection<Agente>> ListarAsync(FiltrosAgentes filtros, CancellationToken cancelamento);
    Task<Agente?> ConsultarAsync(Guid id, CancellationToken cancelamento);
    Task<Agente?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento);
    Task<int> ContarComCoordenadasAsync(CancellationToken cancelamento);
    Task<IReadOnlyCollection<Agente>> ListarComCoordenadasAsync(int deslocamento, int limite, CancellationToken cancelamento);
    Task<IReadOnlyCollection<ContagemSituacaoAgente>> ContarPorSituacaoAsync(CancellationToken cancelamento);
    Task AdicionarAsync(Agente agente, CancellationToken cancelamento);
    Task SalvarAlteracoesAsync(CancellationToken cancelamento);
}
