using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Grupos;

namespace SesionesServicio.Infraestructura.TiempoReal.Hubs;

[Authorize]
public sealed class SesionesHub : Hub
{
    public const string GrupoListadoSesiones = "sesiones:listado";

    public static string GrupoSesion(Guid sesionId) => $"sesion:{sesionId}";
    public static string GrupoEquipo(Guid equipoId) => $"equipo:{equipoId}";
    public static string GrupoOperadoresSesion(Guid sesionId) => $"operadores:sesion:{sesionId}";

    private readonly IServicioGruposSesionesTiempoReal _grupos;
    private readonly IAlmacenUbicaciones _almacen;
    private readonly ILogger<SesionesHub> _logger;

    public SesionesHub(IServicioGruposSesionesTiempoReal grupos, IAlmacenUbicaciones almacen, ILogger<SesionesHub> logger)
    {
        _grupos = grupos;
        _almacen = almacen;
        _logger = logger;
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

    public async Task EnviarUbicacion(string sesionId, string? equipoId, double latitud, double longitud)
    {
        var id = ParsearGuid(sesionId, "la sesión");
        var actor = ObtenerActor();
        if (actor.UsuarioId is null) return;

        Guid? equipoGuid = Guid.TryParse(equipoId, out var eq) ? eq : null;

        _logger.LogInformation(
            "[Ubicacion] recibido: sesion={SesionId} usuario={UserId} ({Nombre}) equipo={EquipoId} lat={Lat} lng={Lng}",
            id, actor.UsuarioId, actor.NombreUsuario, equipoGuid, latitud, longitud);

        _almacen.Actualizar(id, actor.UsuarioId.Value, actor.NombreUsuario ?? string.Empty, equipoGuid, latitud, longitud);

        var dto = new UbicacionActualizadaDto
        {
            SesionId = id,
            ParticipanteIdentidadId = actor.UsuarioId.Value,
            Nombre = actor.NombreUsuario ?? string.Empty,
            EquipoId = equipoGuid,
            Latitud = latitud,
            Longitud = longitud,
            FechaEventoUtc = DateTime.UtcNow
        };

        var grupoOp = GrupoOperadoresSesion(id);
        _logger.LogInformation("[Ubicacion] enviando a grupo={Grupo}", grupoOp);
        var tareas = new List<Task>
        {
            Clients.Group(grupoOp).SendAsync("UbicacionActualizada", dto, Context.ConnectionAborted)
        };

        if (equipoGuid.HasValue)
        {
            var grupoEq = GrupoEquipo(equipoGuid.Value);
            _logger.LogInformation("[Ubicacion] enviando a equipo={Grupo}", grupoEq);
            tareas.Add(Clients.Group(grupoEq).SendAsync("UbicacionActualizada", dto, Context.ConnectionAborted));
        }

        await Task.WhenAll(tareas);
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
