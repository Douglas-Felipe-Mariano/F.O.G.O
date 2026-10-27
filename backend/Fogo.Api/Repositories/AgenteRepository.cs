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

    public async Task<IReadOnlyCollection<HistoricoLocalizacaoAgente>> ListarHistoricoAsync(
        Guid agenteId,
        DateOnly data,
        CancellationToken cancelamento)
    {
        var inicio = new DateTimeOffset(data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var fim = inicio.AddDays(1);
        return await banco.HistoricoLocalizacoesAgentes.AsNoTracking()
            .Where(localizacao => localizacao.AgenteId == agenteId
                && localizacao.RegistradaEm >= inicio
                && localizacao.RegistradaEm < fim)
            .OrderBy(localizacao => localizacao.RegistradaEm)
            .Take(2000)
            .ToArrayAsync(cancelamento);
    }

    public async Task<IReadOnlyCollection<HistoricoLocalizacaoAgente>> ListarHistoricoRecenteAsync(
        Guid agenteId,
        int limite,
        CancellationToken cancelamento) =>
        await banco.HistoricoLocalizacoesAgentes.AsNoTracking()
            .Where(localizacao => localizacao.AgenteId == agenteId)
            .OrderByDescending(localizacao => localizacao.RegistradaEm)
            .ThenByDescending(localizacao => localizacao.Id)
            .Take(limite)
            .ToArrayAsync(cancelamento);

    public Task<int> ContarHistoricoAsync(
        Guid agenteId,
        DateTimeOffset? inicio,
        DateTimeOffset? fimExclusivo,
        CancellationToken cancelamento) =>
        AplicarFiltrosHistorico(banco.HistoricoLocalizacoesAgentes.AsNoTracking(), agenteId, inicio, fimExclusivo)
            .CountAsync(cancelamento);

    public async Task<IReadOnlyCollection<HistoricoLocalizacaoAgente>> ListarHistoricoPaginadoAsync(
        Guid agenteId,
        DateTimeOffset? inicio,
        DateTimeOffset? fimExclusivo,
        int deslocamento,
        int limite,
        CancellationToken cancelamento) =>
        await AplicarFiltrosHistorico(banco.HistoricoLocalizacoesAgentes.AsNoTracking(), agenteId, inicio, fimExclusivo)
            .OrderBy(localizacao => localizacao.RegistradaEm)
            .ThenBy(localizacao => localizacao.Id)
            .Skip(deslocamento)
            .Take(limite)
            .ToArrayAsync(cancelamento);

    public Task AdicionarLocalizacaoHistoricaAsync(HistoricoLocalizacaoAgente localizacao, CancellationToken cancelamento)
    {
        banco.HistoricoLocalizacoesAgentes.Add(localizacao);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<ContagemSituacaoAgente>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
        await banco.Agentes.AsNoTracking()
            .GroupBy(agente => agente.Situacao)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new ContagemSituacaoAgente(grupo.Key, grupo.Count()))
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

    private static IQueryable<HistoricoLocalizacaoAgente> AplicarFiltrosHistorico(
        IQueryable<HistoricoLocalizacaoAgente> consulta,
        Guid agenteId,
        DateTimeOffset? inicio,
        DateTimeOffset? fimExclusivo)
    {
        consulta = consulta.Where(localizacao => localizacao.AgenteId == agenteId);
        if (inicio.HasValue)
        {
            consulta = consulta.Where(localizacao => localizacao.RegistradaEm >= inicio.Value);
        }

        if (fimExclusivo.HasValue)
        {
            consulta = consulta.Where(localizacao => localizacao.RegistradaEm < fimExclusivo.Value);
        }

        return consulta;
    }
}
