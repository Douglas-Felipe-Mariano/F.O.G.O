using Microsoft.AspNetCore.SignalR;

namespace Fogo.Api.Application.Inventario;

public sealed class PublicadorEventosEquipamentos(
    IHubContext<EquipamentosHub> hub,
    ILogger<PublicadorEventosEquipamentos> logger)
{
    public async Task PublicarAlteracaoAsync(
        Guid equipamentoId,
        string operacao,
        DateTimeOffset atualizadoEm,
        CancellationToken cancelamento)
    {
        try
        {
            await hub.Clients.All.SendAsync(
                "equipamentoAlterado",
                new EventoEquipamentoAlterado(equipamentoId, operacao, atualizadoEm),
                cancelamento);
        }
        catch (Exception excecao)
        {
            logger.LogWarning(excecao, "Não foi possível publicar evento SignalR para equipamento {EquipamentoId}.", equipamentoId);
        }
    }
}

public sealed record EventoEquipamentoAlterado(Guid EquipamentoId, string Operacao, DateTimeOffset AtualizadoEm);