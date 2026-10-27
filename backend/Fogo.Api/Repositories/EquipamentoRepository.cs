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

    public Task<int> ContarComCoordenadasAsync(
        FiltrosMapaEquipamentos filtros,
        CancellationToken cancelamento) =>
        AplicarFiltrosMapa(banco.Equipamentos.AsNoTracking(), filtros)
            .CountAsync(item => item.Latitude != null && item.Longitude != null, cancelamento);

    public async Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(
        FiltrosMapaEquipamentos filtros,
        int deslocamento,
        int limite,
        CancellationToken cancelamento) =>
        await AplicarFiltrosMapa(banco.Equipamentos.AsNoTracking(), filtros)
            .Where(item => item.Latitude != null && item.Longitude != null)
            .OrderBy(item => item.Id)
            .Skip(deslocamento)
            .Take(limite)
            .ToArrayAsync(cancelamento);

    public async Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
        await banco.Equipamentos.AsNoTracking()
            .GroupBy(item => item.Situacao)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new ContagemSituacao(grupo.Key, grupo.Count()))
            .ToArrayAsync(cancelamento);

    public Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking().CountAsync(item => item.NecessitaManutencao, cancelamento);

    public Task<int> ContarManutencoesImediatasAsync(CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking().CountAsync(
            item => item.NecessitaManutencao && item.TipoManutencao == TipoManutencao.Imediata,
            cancelamento);

    public Task<int> ContarManutencoesComDataAteAsync(DateOnly limite, CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking().CountAsync(
            item => item.NecessitaManutencao
                && item.TipoManutencao != TipoManutencao.Imediata
                && item.ProximaManutencao < limite,
            cancelamento);

    public Task<int> ContarManutencoesEntreAsync(DateOnly inicio, DateOnly fim, CancellationToken cancelamento) =>
        banco.Equipamentos.AsNoTracking().CountAsync(
            item => item.NecessitaManutencao
                && item.TipoManutencao != TipoManutencao.Imediata
                && item.ProximaManutencao >= inicio
                && item.ProximaManutencao <= fim,
            cancelamento);

    public async Task<IReadOnlyCollection<ResumoOperacionalLocalidade>> ConsultarResumoPorLocalidadeAsync(
        DateOnly hoje,
        DateOnly fimJanelaAviso,
        CancellationToken cancelamento) =>
        await banco.Equipamentos.AsNoTracking()
            .GroupBy(item => new { item.Bairro, item.Cidade, item.Uf })
            .Select(grupo => new ResumoOperacionalLocalidade(
                grupo.Key.Bairro,
                grupo.Key.Cidade,
                grupo.Key.Uf,
                grupo.Count(),
                grupo.Count(item => item.Situacao != SituacaoEquipamento.AindaNaoInstalado),
                grupo.Count(item => item.Situacao == SituacaoEquipamento.Operacional),
                grupo.Count(item => item.Situacao != SituacaoEquipamento.AindaNaoInstalado
                    && item.Situacao != SituacaoEquipamento.Operacional),
                grupo.Count(item => item.NecessitaManutencao && item.TipoManutencao == TipoManutencao.Imediata),
                grupo.Count(item => item.NecessitaManutencao
                    && item.TipoManutencao != TipoManutencao.Imediata
                    && item.ProximaManutencao < hoje),
                grupo.Count(item => item.NecessitaManutencao
                    && item.TipoManutencao != TipoManutencao.Imediata
                    && item.ProximaManutencao >= hoje
                    && item.ProximaManutencao <= fimJanelaAviso)))
            .OrderBy(item => item.Cidade)
            .ThenBy(item => item.Bairro)
            .ThenBy(item => item.Uf)
            .ToArrayAsync(cancelamento);

    public async Task<IReadOnlyCollection<ProjecaoManutencaoLocalidade>> ConsultarProjecaoManutencaoAsync(
        DateOnly hoje,
        DateOnly fimHorizonte,
        CancellationToken cancelamento) =>
        await banco.Equipamentos.AsNoTracking()
            .Where(item => item.NecessitaManutencao
                && item.TipoManutencao != TipoManutencao.Imediata
                && item.ProximaManutencao >= hoje
                && item.ProximaManutencao <= fimHorizonte)
            .GroupBy(item => new { item.Bairro, item.Cidade, item.Uf, Data = item.ProximaManutencao!.Value })
            .Select(grupo => new ProjecaoManutencaoLocalidade(
                grupo.Key.Bairro,
                grupo.Key.Cidade,
                grupo.Key.Uf,
                grupo.Key.Data,
                grupo.Count()))
            .OrderBy(item => item.Data)
            .ThenBy(item => item.Cidade)
            .ThenBy(item => item.Bairro)
            .ToArrayAsync(cancelamento);

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

        if (filtros.NecessitaManutencao.HasValue)
        {
            consulta = consulta.Where(item => item.NecessitaManutencao == filtros.NecessitaManutencao.Value);
        }

        return consulta;
    }

    private static IQueryable<Equipamento> AplicarFiltrosMapa(
        IQueryable<Equipamento> consulta,
        FiltrosMapaEquipamentos filtros)
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
