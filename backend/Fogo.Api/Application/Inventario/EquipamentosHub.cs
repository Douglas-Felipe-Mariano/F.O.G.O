using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fogo.Api.Application.Inventario;

[Authorize]
public sealed class EquipamentosHub : Hub
{
}