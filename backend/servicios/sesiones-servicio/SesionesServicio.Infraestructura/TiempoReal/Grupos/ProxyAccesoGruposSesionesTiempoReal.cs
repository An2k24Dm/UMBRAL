using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.TiempoReal.Grupos;

// Proxy de protección (patrón Proxy GoF): implementa la misma interfaz que el
// sujeto real, valida la autorización del actor y, solo si procede, delega en
// el sujeto real. Si el acceso se deniega, el sujeto real nunca es invocado.
//
// Reconstruye internamente ContextoActorTiempoReal a partir de la identidad
// primitiva recibida para reutilizar sus comprobaciones de rol.
//
// Reglas (respetan las autorizaciones ya existentes del proyecto):
//  - Listado: cualquier actor con rol reconocido (Administrador/Operador/Participante).
//  - Sesión:  Administrador → cualquiera; Operador → la que creó; Participante → a la que pertenece.
//  - Equipo:  Administrador → cualquiera; Operador → equipos de sus sesiones; Participante → su equipo.
public sealed class ProxyAccesoGruposSesionesTiempoReal : IServicioGruposSesionesTiempoReal
{
    private readonly IServicioGruposSesionesTiempoReal _real;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly ILogger<ProxyAccesoGruposSesionesTiempoReal> _log;

    public ProxyAccesoGruposSesionesTiempoReal(
        IServicioGruposSesionesTiempoReal real,
        IRepositorioSesiones repositorioSesiones,
        ILogger<ProxyAccesoGruposSesionesTiempoReal> log)
    {
        _real = real;
        _repositorioSesiones = repositorioSesiones;
        _log = log;
    }

    public Task UnirseAListadoAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        CancellationToken cancelacion)
    {
        var actor = new ContextoActorTiempoReal(usuarioId, roles, null);
        if (!actor.TieneRolReconocido())
            throw Denegar("Listado", null, actor,
                "No tiene permiso para suscribirse al listado de sesiones.");

        return _real.UnirseAListadoAsync(connectionId, usuarioId, roles, cancelacion);
    }

    public Task SalirDeListadoAsync(string connectionId, CancellationToken cancelacion)
        => _real.SalirDeListadoAsync(connectionId, cancelacion);

    public async Task UnirseASesionAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        Guid sesionId, CancellationToken cancelacion)
    {
        var actor = new ContextoActorTiempoReal(usuarioId, roles, null);
        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(sesionId, cancelacion);
        if (sesion is null || !PuedeAccederASesion(sesion, actor))
            throw Denegar("Sesion", sesionId, actor,
                "No tiene permiso para suscribirse a esta sesión.");

        await _real.UnirseASesionAsync(connectionId, usuarioId, roles, sesionId, cancelacion);
    }

    public Task SalirDeSesionAsync(string connectionId, Guid sesionId, CancellationToken cancelacion)
        => _real.SalirDeSesionAsync(connectionId, sesionId, cancelacion);

    public async Task UnirseAEquipoAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        Guid equipoId, CancellationToken cancelacion)
    {
        var actor = new ContextoActorTiempoReal(usuarioId, roles, null);

        // El equipo debe existir y su sesión propietaria se resuelve desde el
        // repositorio, no desde datos del frontend.
        var sesion = await _repositorioSesiones.ObtenerPorEquipoIdAsync(equipoId, cancelacion);
        if (sesion is not SesionGrupal grupal)
            throw Denegar("Equipo", equipoId, actor,
                "No tiene permiso para suscribirse a este equipo.");

        var equipo = grupal.Equipos.FirstOrDefault(e => e.Id == equipoId);
        if (equipo is null || !PuedeAccederAEquipo(grupal, equipo, actor))
            throw Denegar("Equipo", equipoId, actor,
                "No tiene permiso para suscribirse a este equipo.");

        await _real.UnirseAEquipoAsync(connectionId, usuarioId, roles, equipoId, cancelacion);
    }

    public Task SalirDeEquipoAsync(string connectionId, Guid equipoId, CancellationToken cancelacion)
        => _real.SalirDeEquipoAsync(connectionId, equipoId, cancelacion);

    private static bool PuedeAccederASesion(Sesion sesion, ContextoActorTiempoReal actor)
    {
        if (actor.EsAdministrador) return true;
        if (actor.EsOperador)
            return actor.UsuarioId is Guid uid && sesion.OperadorCreadorId == uid;
        if (actor.EsParticipante && actor.UsuarioId is Guid pid)
        {
            return sesion switch
            {
                SesionIndividual individual =>
                    individual.Participantes.Any(p => p.ParticipanteIdentidadId == pid),
                SesionGrupal grupal =>
                    grupal.Equipos.Any(e => e.ContieneParticipanteIdentidadId(pid)),
                _ => false
            };
        }
        return false;
    }

    private static bool PuedeAccederAEquipo(
        SesionGrupal grupal, Equipo equipo, ContextoActorTiempoReal actor)
    {
        if (actor.EsAdministrador) return true;
        if (actor.EsOperador)
            return actor.UsuarioId is Guid uid && grupal.OperadorCreadorId == uid;
        if (actor.EsParticipante && actor.UsuarioId is Guid pid)
            return equipo.ContieneParticipanteIdentidadId(pid);
        return false;
    }

    // Registra un intento denegado (sin token ni datos sensibles) y devuelve la
    // excepción para que el llamador la lance (throw Denegar(...)).
    private HubException Denegar(
        string tipoGrupo, Guid? recursoId, ContextoActorTiempoReal actor, string mensaje)
    {
        _log.LogWarning(
            "IntentoAccesoGrupoSignalRDenegado UsuarioId={UsuarioId} Rol={Rol} TipoGrupo={TipoGrupo} RecursoId={RecursoId}",
            actor.UsuarioId, actor.RolPrincipal(), tipoGrupo, recursoId);
        return new HubException(mensaje);
    }
}
