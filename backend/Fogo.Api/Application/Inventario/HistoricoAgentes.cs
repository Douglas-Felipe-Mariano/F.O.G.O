using Fogo.Api.Inventario;
using Fogo.Api.Repositories;

namespace Fogo.Api.Application.Inventario;

public sealed class HistoricoAgentesService(IAgenteRepository repositorio)
{
    public async Task<HistoricoAgentePaginadoResposta?> ConsultarHistoricoPaginadoAsync(
        Guid agenteId,
        DateOnly? inicio,
        DateOnly? fim,
        int deslocamento,
        int limite,
        CancellationToken cancelamento)
    {
        var agente = await repositorio.ConsultarAsync(agenteId, cancelamento);
        if (agente is null)
        {
            return null;
        }

        DateTimeOffset? inicioUtc = inicio.HasValue
            ? new DateTimeOffset(inicio.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        DateTimeOffset? fimExclusivoUtc = fim.HasValue
            ? new DateTimeOffset(fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        var total = await repositorio.ContarHistoricoAsync(agenteId, inicioUtc, fimExclusivoUtc, cancelamento);
        var registros = await repositorio.ListarHistoricoPaginadoAsync(
            agenteId, inicioUtc, fimExclusivoUtc, deslocamento, limite, cancelamento);
        var demonstrativo = agente.OrigemDados == OrigemRegistro.Simulado;
        var localizacoes = registros.Select(localizacao => new HistoricoLocalizacaoAgenteResposta(
            localizacao.Id,
            localizacao.AgenteId,
            localizacao.Latitude,
            localizacao.Longitude,
            localizacao.RegistradaEm)).ToArray();

        return new HistoricoAgentePaginadoResposta(
            agenteId,
            agente.Nome,
            agente.OrigemDados,
            deslocamento,
            limite,
            total,
            (long)deslocamento + localizacoes.Length < total,
            total == 0
                ? "Não há registros de localização para este agente no período selecionado."
                : demonstrativo
                    ? "Registro demonstrativo baseado na posição cadastrada; não representa deslocamento real."
                    : "Registros históricos armazenados; não representam transmissão em tempo real.",
            localizacoes);
    }

    public async Task<UltimasLocalizacoesAgenteResposta?> ConsultarUltimasLocalizacoesAsync(
        Guid agenteId,
        int limite,
        CancellationToken cancelamento)
    {
        var agente = await repositorio.ConsultarAsync(agenteId, cancelamento);
        if (agente is null)
        {
            return null;
        }

        var recentes = await repositorio.ListarHistoricoRecenteAsync(agenteId, limite, cancelamento);
        var localizacoes = recentes
            .OrderBy(localizacao => localizacao.RegistradaEm)
            .ThenBy(localizacao => localizacao.Id)
            .Select(localizacao => new HistoricoLocalizacaoAgenteResposta(
                localizacao.Id,
                localizacao.AgenteId,
                localizacao.Latitude,
                localizacao.Longitude,
                localizacao.RegistradaEm))
            .ToArray();
        var demonstrativo = agente.OrigemDados == OrigemRegistro.Simulado;

        return new UltimasLocalizacoesAgenteResposta(
            agenteId,
            agente.Nome,
            agente.OrigemDados,
            localizacoes.Length > 0,
            localizacoes.Length == 0
                ? "Não há registros de localização armazenados para este agente."
                : demonstrativo
                    ? "Registro demonstrativo baseado na posição cadastrada; não representa deslocamento real."
                    : "Últimos registros gravados no histórico; não representam transmissão em tempo real.",
            localizacoes);
    }

    public async Task<TrajetoAgenteResposta?> ConsultarTrajetoDiarioAsync(
        Guid agenteId,
        DateOnly data,
        CancellationToken cancelamento)
    {
        var agente = await repositorio.ConsultarAsync(agenteId, cancelamento);
        if (agente is null)
        {
            return null;
        }

        var localizacoes = await repositorio.ListarHistoricoAsync(agenteId, data, cancelamento);
        var disponivel = localizacoes.Count > 0;
        return new TrajetoAgenteResposta(
            agenteId,
            agente.Nome,
            data,
            disponivel,
            disponivel
                ? "Histórico de posições registradas no período selecionado."
                : "Não há histórico de deslocamento para este agente e data. A posição cadastrada não constitui rastreamento.",
            localizacoes.Select(localizacao => new HistoricoLocalizacaoAgenteResposta(
                localizacao.Id,
                localizacao.AgenteId,
                localizacao.Latitude,
                localizacao.Longitude,
                localizacao.RegistradaEm)).ToArray());
    }
}