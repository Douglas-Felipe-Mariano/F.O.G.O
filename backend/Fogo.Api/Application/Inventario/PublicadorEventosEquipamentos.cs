using Microsoft.AspNetCore.SignalR;

namespace Fogo.Api.Application.Inventario;

public interface IPublicadorEventosEquipamentos
{
    Task PublicarAlteracaoAsync(
        Guid equipamentoId,
        string operacao,
        DateTimeOffset atualizadoEm,
        CancellationToken cancelamento);
}

public sealed class PublicadorEventosEquipamentos(
    IHubContext<EquipamentosHub> hub,
    ILogger<PublicadorEventosEquipamentos> logger) : IPublicadorEventosEquipamentos
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

public interface IPublicadorEventosAgentes
{
    Task PublicarAlteracaoAsync(Guid agenteId, string operacao, DateTimeOffset atualizadoEm, CancellationToken cancelamento);
}

public sealed class PublicadorEventosAgentes(
    IHubContext<EquipamentosHub> hub,
    ILogger<PublicadorEventosAgentes> logger) : IPublicadorEventosAgentes
{
    public async Task PublicarAlteracaoAsync(
        Guid agenteId,
        string operacao,
        DateTimeOffset atualizadoEm,
        CancellationToken cancelamento)
    {
        try
        {
            await hub.Clients.All.SendAsync(
                "agenteAlterado",
                new EventoAgenteAlterado(agenteId, operacao, atualizadoEm),
                cancelamento);
        }
        catch (Exception excecao)
        {
            logger.LogWarning(excecao, "Não foi possível publicar evento SignalR para agente {AgenteId}.", agenteId);
        }
    }
}

public sealed record EventoAgenteAlterado(Guid AgenteId, string Operacao, DateTimeOffset AtualizadoEm);