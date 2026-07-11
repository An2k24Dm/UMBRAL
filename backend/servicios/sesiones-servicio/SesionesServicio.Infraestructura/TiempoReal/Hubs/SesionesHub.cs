using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.TiempoReal.Grupos;

namespace SesionesServicio.Infraestructura.TiempoReal.Hubs;

[Authorize]
public sealed class SesionesHub : Hub
{
    public const string GrupoListadoSesiones = "sesiones:listado";

    public static string GrupoSesion(Guid sesionId) => $"sesion:{sesionId}";

    public static string GrupoEquipo(Guid equipoId) => $"equipo:{equipoId}";

    private readonly IServicioGruposSesionesTiempoReal _grupos;

    public SesionesHub(IServicioGruposSesionesTiempoReal grupos)
    {
        _grupos = grupos;
    }

    public Task UnirseAListadoSesiones()
    {
        var actor = ObtenerActor();
        return _grupos.UnirseAListadoAsync(
            Context.ConnectionId, actor.UsuarioId, actor.Roles, Context.ConnectionAborted);
    }

    public Task SalirDeListadoSesiones()
        => _grupos.SalirDeListadoAsync(Context.ConnectionId, Context.ConnectionAborted);

    public Task UnirseASesion(string sesionId)
    {
        var id = ParsearGuid(sesionId, "la sesión");
        var actor = ObtenerActor();
        return _grupos.UnirseASesionAsync(
            Context.ConnectionId, actor.UsuarioId, actor.Roles, id, Context.ConnectionAborted);
    }

    public Task SalirDeSesion(string sesionId)
    {
        if (!Guid.TryParse(sesionId, out var id)) return Task.CompletedTask;
        return _grupos.SalirDeSesionAsync(Context.ConnectionId, id, Context.ConnectionAborted);
    }

    public Task UnirseAEquipo(string equipoId)
    {
        var id = ParsearGuid(equipoId, "el equipo");
        var actor = ObtenerActor();
        return _grupos.UnirseAEquipoAsync(
            Context.ConnectionId, actor.UsuarioId, actor.Roles, id, Context.ConnectionAborted);
    }

    public Task SalirDeEquipo(string equipoId)
    {
        if (!Guid.TryParse(equipoId, out var id)) return Task.CompletedTask;
        return _grupos.SalirDeEquipoAsync(Context.ConnectionId, id, Context.ConnectionAborted);
    }

    private static Guid ParsearGuid(string valor, string recurso)
        => Guid.TryParse(valor, out var id)
            ? id
            : throw new HubException($"El identificador de {recurso} no es válido.");

    private ContextoActorTiempoReal ObtenerActor()
    {
        var usuario = Context.User;
        var sub = Context.UserIdentifier
                  ?? usuario?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? usuario?.FindFirstValue("sub");

        Guid? usuarioId = Guid.TryParse(sub, out var id) ? id : null;

        var roles = usuario?.FindAll("roles")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray() ?? Array.Empty<string>();

        var nombre = usuario?.FindFirstValue("preferred_username") ?? usuario?.Identity?.Name;

        return new ContextoActorTiempoReal(usuarioId, roles, nombre);
    }
}
