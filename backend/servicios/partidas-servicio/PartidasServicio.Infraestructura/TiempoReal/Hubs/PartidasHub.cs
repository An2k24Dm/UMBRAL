using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PartidasServicio.Infraestructura.TiempoReal.Hubs;

[Authorize]
public sealed class PartidasHub : Hub
{
    public static string GrupoPartida(Guid sesionId) => $"partida:{sesionId}";

    public Task UnirseAPartida(string sesionId)
    {
        if (!Guid.TryParse(sesionId, out var id))
            return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, GrupoPartida(id));
    }

    public Task SalirDePartida(string sesionId)
    {
        if (!Guid.TryParse(sesionId, out var id))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPartida(id));
    }
}
