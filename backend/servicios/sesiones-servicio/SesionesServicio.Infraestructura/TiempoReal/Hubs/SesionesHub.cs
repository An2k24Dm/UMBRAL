using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SesionesServicio.Infraestructura.TiempoReal.Hubs;

[Authorize]
public sealed class SesionesHub : Hub
{
    public const string GrupoListadoSesiones = "sesiones:listado";

    public static string GrupoSesion(Guid sesionId) => $"sesion:{sesionId}";

    public static string GrupoEquipo(Guid equipoId) => $"equipo:{equipoId}";

    public Task UnirseAListadoSesiones()
        => Groups.AddToGroupAsync(Context.ConnectionId, GrupoListadoSesiones);

    public Task SalirDeListadoSesiones()
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoListadoSesiones);

    public Task UnirseASesion(string sesionId)
    {
        if (!Guid.TryParse(sesionId, out var id))
            return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, GrupoSesion(id));
    }

    public Task SalirDeSesion(string sesionId)
    {
        if (!Guid.TryParse(sesionId, out var id))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoSesion(id));
    }

    public Task UnirseAEquipo(string equipoId)
    {
        if (!Guid.TryParse(equipoId, out var id))
            return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, GrupoEquipo(id));
    }

    public Task SalirDeEquipo(string equipoId)
    {
        if (!Guid.TryParse(equipoId, out var id))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoEquipo(id));
    }
}
