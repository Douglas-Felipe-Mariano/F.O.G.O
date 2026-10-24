using Fogo.Api.Identity;
using Fogo.Api.Inventario;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api.Repositories;

public sealed class EquipamentoRepository(FogoDbContext banco) : IEquipamentoRepository
{
    public Task<int> ContarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento) =>
        AplicarFiltros(banco.Equipamentos.AsNoTracking(), filtros).CountAsync(cancelamento);

    public async Task<IReadOnlyCollection<Equipamento>> ListarAsync(
        FiltrosEquipamentos filtros,
        CancellationToken cancelamento)
    {
        var deslocamento = (int)((long)(filtros.Pagina - 1) * filtros.TamanhoPagina);
        return await AplicarFiltros(banco.Equipamentos.AsNoTracking(), filtros)
            .OrderBy(item => item.Tipo)
            .ThenBy(item => item.NumeroSerie)
            .ThenBy(item => item.Id)
            .Skip(deslocamento)
            .Take(filtros.TamanhoPagina)
            .ToArrayAsync(cancelamento);
    }

    public Task<Equipamento?> ConsultarAsync(Guid id, CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancelamento);

    public Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento) =>
        banco.Equipamentos.SingleOrDefaultAsync(item => item.Id == id, cancelamento);

    public Task<int> ContarComCoordenadasAsync(CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking()
            .CountAsync(item => item.Latitude != null && item.Longitude != null, cancelamento);

    public async Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(
        int limite,
        CancellationToken cancelamento) =>
        await banco.Equipamentos.AsNoTracking()
            .Where(item => item.Latitude != null && item.Longitude != null)
            .OrderBy(item => item.Id)
            .Take(limite)
            .ToArrayAsync(cancelamento);

    public async Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
        await banco.Equipamentos.AsNoTracking()
            .GroupBy(item => item.Situacao)
            .Select(grupo => new ContagemSituacao(grupo.Key, grupo.Count()))
            .OrderBy(item => item.Situacao)
            .ToArrayAsync(cancelamento);

    public Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking().CountAsync(item => item.NecessitaManutencao, cancelamento);

    public async Task AdicionarAsync(Equipamento equipamento, CancellationToken cancelamento)
    {
        banco.Equipamentos.Add(equipamento);
        await banco.SaveChangesAsync(cancelamento);
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancelamento) => banco.SaveChangesAsync(cancelamento);

    private static IQueryable<Equipamento> AplicarFiltros(
        IQueryable<Equipamento> consulta,
        FiltrosEquipamentos filtros)
    {
        if (!string.IsNullOrWhiteSpace(filtros.Tipo))
        {
            consulta = consulta.Where(item => item.Tipo == filtros.Tipo.Trim());
        }

        if (filtros.Situacao.HasValue)
        {
            consulta = consulta.Where(item => item.Situacao == filtros.Situacao.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtros.Bairro))
        {
            consulta = consulta.Where(item => item.Bairro == filtros.Bairro.Trim());
        }

        return consulta;
    }
}
