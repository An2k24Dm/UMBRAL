using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RankingServicio.Infraestructura.TiempoReal;

[Authorize]
public sealed class RankingHub : Hub
{
    public async Task UnirseASesion(string sesionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sesion:{sesionId}");
    }

    public async Task SalirDeSesion(string sesionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sesion:{sesionId}");
    }
}
