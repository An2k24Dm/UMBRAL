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

    public override Task OnConnectedAsync()
    {
        var actor = ObtenerActor();
        _logger.LogInformation(
            "SignalR Sesiones conectado. ConnectionId={ConnectionId} UsuarioId={UsuarioId} Usuario={Usuario} Roles={Roles}",
            Context.ConnectionId,
            actor.UsuarioId,
            actor.NombreUsuario,
            string.Join(",", actor.Roles));
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var usuarioId = ObtenerActor().UsuarioId;
        if (exception is null)
        {
            _logger.LogInformation(
                "SignalR Sesiones desconectado. ConnectionId={ConnectionId} UsuarioId={UsuarioId}",
                Context.ConnectionId,
                usuarioId);
        }
        else if (EsDesconexionTransitoria(exception))
        {
            _logger.LogWarning(
                "SignalR Sesiones desconexión transitoria. ConnectionId={ConnectionId} UsuarioId={UsuarioId} TipoExcepcion={TipoExcepcion} Mensaje={Mensaje}",
                Context.ConnectionId,
                usuarioId,
                exception.GetType().Name,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "SignalR Sesiones desconexión inesperada. ConnectionId={ConnectionId} UsuarioId={UsuarioId} TipoExcepcion={TipoExcepcion}",
                Context.ConnectionId,
                usuarioId,
                exception.GetType().Name);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task UnirseAListadoSesiones()
    {
        var actor = ObtenerActor();
        await _grupos.UnirseAListadoAsync(
            Context.ConnectionId, actor.UsuarioId, actor.Roles, Context.ConnectionAborted);
        _logger.LogInformation(
            "SignalR Sesiones unido al listado. ConnectionId={ConnectionId} UsuarioId={UsuarioId}",
            Context.ConnectionId,
            actor.UsuarioId);
    }

    public async Task SalirDeListadoSesiones()
    {
        await _grupos.SalirDeListadoAsync(Context.ConnectionId, Context.ConnectionAborted);
        _logger.LogDebug(
            "SignalR Sesiones salió del listado. ConnectionId={ConnectionId}",
            Context.ConnectionId);
    }

    public async Task UnirseASesion(string sesionId)
    {
        var id = ParsearGuid(sesionId, "la sesión");
        var actor = ObtenerActor();
        await _grupos.UnirseASesionAsync(
            Context.ConnectionId, actor.UsuarioId, actor.Roles, id, Context.ConnectionAborted);
        _logger.LogInformation(
            "SignalR Sesiones unido a sesión. ConnectionId={ConnectionId} UsuarioId={UsuarioId} SesionId={SesionId}",
            Context.ConnectionId,
            actor.UsuarioId,
            id);
    }

    public async Task SalirDeSesion(string sesionId)
    {
        if (!Guid.TryParse(sesionId, out var id)) return;
        await _grupos.SalirDeSesionAsync(Context.ConnectionId, id, Context.ConnectionAborted);
        _logger.LogDebug(
            "SignalR Sesiones salió de sesión. ConnectionId={ConnectionId} SesionId={SesionId}",
            Context.ConnectionId,
            id);
    }

    public async Task UnirseAEquipo(string equipoId)
    {
        var id = ParsearGuid(equipoId, "el equipo");
        var actor = ObtenerActor();
        await _grupos.UnirseAEquipoAsync(
            Context.ConnectionId, actor.UsuarioId, actor.Roles, id, Context.ConnectionAborted);
        _logger.LogInformation(
            "SignalR Sesiones unido a equipo. ConnectionId={ConnectionId} UsuarioId={UsuarioId} EquipoId={EquipoId}",
            Context.ConnectionId,
            actor.UsuarioId,
            id);
    }

    public async Task SalirDeEquipo(string equipoId)
    {
        if (!Guid.TryParse(equipoId, out var id)) return;
        await _grupos.SalirDeEquipoAsync(Context.ConnectionId, id, Context.ConnectionAborted);
        _logger.LogDebug(
            "SignalR Sesiones salió de equipo. ConnectionId={ConnectionId} EquipoId={EquipoId}",
            Context.ConnectionId,
            id);
    }

    public async Task EnviarUbicacion(string sesionId, string? equipoId, double latitud, double longitud)
    {
        var id = ParsearGuid(sesionId, "la sesión");
        var actor = ObtenerActor();
        if (actor.UsuarioId is null)
        {
            _logger.LogWarning(
                "[Ubicacion] coordenada descartada: usuario no identificado. ConnectionId={ConnectionId} SesionId={SesionId}",
                Context.ConnectionId, id);
            return;
        }

        Guid? equipoGuid = Guid.TryParse(equipoId, out var eq) ? eq : null;

        // Una ubicación llega cada ~5s por participante: se registra en Debug para
        // no saturar Information en producción (ver requerimiento de observabilidad).
        _logger.LogDebug(
            "[Ubicacion] recibido: sesion={SesionId} usuario={UserId} equipo={EquipoId}",
            id, actor.UsuarioId, equipoGuid);

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
        _logger.LogDebug("[Ubicacion] enviando a grupo={Grupo}", grupoOp);
        var tareas = new List<Task>
        {
            Clients.Group(grupoOp).SendAsync("UbicacionActualizada", dto, Context.ConnectionAborted)
        };

        if (equipoGuid.HasValue)
        {
            var grupoEq = GrupoEquipo(equipoGuid.Value);
            _logger.LogDebug("[Ubicacion] enviando a equipo={Grupo}", grupoEq);
            tareas.Add(Clients.Group(grupoEq).SendAsync("UbicacionActualizada", dto, Context.ConnectionAborted));
        }

        await Task.WhenAll(tareas);
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

    // Cierres de transporte que la reconexión automática del cliente resuelve:
    // se registran como Warning, no como Error terminal. ConnectionAbortedException
    // deriva de OperationCanceledException, por lo que queda cubierta.
    private static bool EsDesconexionTransitoria(Exception exception)
        => exception is OperationCanceledException
            or System.IO.IOException
            or System.Net.WebSockets.WebSocketException;

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
