using Fogo.Api.Inventario;
using Fogo.Api.Repositories;

namespace Fogo.Api.Application.Inventario;

public sealed class HistoricoAgentesService(IAgenteRepository repositorio)
{
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