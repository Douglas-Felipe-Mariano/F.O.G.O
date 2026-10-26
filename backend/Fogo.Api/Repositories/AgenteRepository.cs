using Fogo.Api.Identity;
using Fogo.Api.Inventario;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api.Repositories;

public sealed class AgenteRepository(FogoDbContext banco) : IAgenteRepository
{
    public Task<int> ContarAsync(FiltrosAgentes filtros, CancellationToken cancelamento) =>
        AplicarFiltros(banco.Agentes.AsNoTracking(), filtros).CountAsync(cancelamento);

    public async Task<IReadOnlyCollection<Agente>> ListarAsync(FiltrosAgentes filtros, CancellationToken cancelamento)
    {
        var deslocamento = (int)((long)(filtros.Pagina - 1) * filtros.TamanhoPagina);
        return await AplicarFiltros(banco.Agentes.AsNoTracking(), filtros)
            .OrderBy(agente => agente.Nome)
            .ThenBy(agente => agente.Id)
            .Skip(deslocamento)
            .Take(filtros.TamanhoPagina)
            .ToArrayAsync(cancelamento);
    }

    public Task<Agente?> ConsultarAsync(Guid id, CancellationToken cancelamento) =>
        banco.Agentes.AsNoTracking().SingleOrDefaultAsync(agente => agente.Id == id, cancelamento);

    public Task<Agente?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento) =>
        banco.Agentes.SingleOrDefaultAsync(agente => agente.Id == id, cancelamento);

    public Task<int> ContarComCoordenadasAsync(CancellationToken cancelamento) =>
        banco.Agentes.AsNoTracking().CountAsync(agente => agente.Latitude != null && agente.Longitude != null, cancelamento);

    public async Task<IReadOnlyCollection<Agente>> ListarComCoordenadasAsync(int deslocamento, int limite, CancellationToken cancelamento) =>
        await banco.Agentes.AsNoTracking()
            .Where(agente => agente.Latitude != null && agente.Longitude != null)
            .OrderBy(agente => agente.Id)
            .Skip(deslocamento)
            .Take(limite)
            .ToArrayAsync(cancelamento);

    public async Task<IReadOnlyCollection<ContagemSituacaoAgente>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
        await banco.Agentes.AsNoTracking()
            .GroupBy(agente => agente.Situacao)
            .Select(grupo => new ContagemSituacaoAgente(grupo.Key, grupo.Count()))
            .OrderBy(item => item.Situacao)
            .ToArrayAsync(cancelamento);

    public async Task AdicionarAsync(Agente agente, CancellationToken cancelamento)
    {
        banco.Agentes.Add(agente);
        await banco.SaveChangesAsync(cancelamento);
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancelamento) => banco.SaveChangesAsync(cancelamento);

    private static IQueryable<Agente> AplicarFiltros(IQueryable<Agente> consulta, FiltrosAgentes filtros)
    {
        if (!string.IsNullOrWhiteSpace(filtros.CategoriaProfissional))
        {
            consulta = consulta.Where(agente => agente.CategoriaProfissional == filtros.CategoriaProfissional.Trim());
        }

        if (filtros.Situacao.HasValue)
        {
            consulta = consulta.Where(agente => agente.Situacao == filtros.Situacao.Value);
        }

        return consulta;
    }
}
