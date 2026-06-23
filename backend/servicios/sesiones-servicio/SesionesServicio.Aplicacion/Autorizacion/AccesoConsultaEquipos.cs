using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Autorizacion;

internal static class AccesoConsultaEquipos
{
    private static readonly EstadoSesion[] EstadosDisponibles =
    {
        EstadoSesion.Programada,
        EstadoSesion.EnPreparacion,
        EstadoSesion.Activa
    };

    private const string MensajeSinPermiso =
        "No tienes permisos para consultar los equipos de esta sesión.";

    public static async Task<(SesionGrupal Sesion, Guid? UsuarioId)> ResolverSesionAutorizadaAsync(
        Guid sesionId,
        IRepositorioSesiones repositorio,
        IUsuarioActual usuarioActual,
        CancellationToken cancelacion)
    {
        var sesion = await repositorio.ObtenerPorIdAsync(sesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionGrupal grupal)
            throw new SesionNoGrupalExcepcion(
                "Solo se pueden consultar equipos de sesiones grupales.");

        var usuarioId = usuarioActual.ObtenerId();

        if (usuarioActual.TieneAlgunRol("Operador"))
        {
            if (usuarioId is not Guid op || grupal.OperadorCreadorId != op)
                throw new AccesoSesionNoPermitidoExcepcion(MensajeSinPermiso);
        }
        else if (usuarioActual.TieneAlgunRol("Participante"))
        {
            var pertenece = usuarioId is Guid pid
                && grupal.Equipos.Any(e => e.ContieneParticipanteIdentidadId(pid));
            if (!pertenece && !EstadosDisponibles.Contains(grupal.Estado))
                throw new AccesoSesionNoPermitidoExcepcion(MensajeSinPermiso);
        }
        else
        {
            throw new AccesoSesionNoPermitidoExcepcion(MensajeSinPermiso);
        }

        return (grupal, usuarioId);
    }

    public static bool EsMiEquipo(Equipo equipo, Guid? usuarioId)
        => usuarioId is Guid pid && equipo.ContieneParticipanteIdentidadId(pid);

    public static bool SoyLider(Equipo equipo, Guid? usuarioId)
    {
        if (usuarioId is not Guid pid) return false;
        var miParticipante = equipo.Participantes
            .FirstOrDefault(p => p.ParticipanteIdentidadId == pid);
        return miParticipante is not null && equipo.LiderParticipanteId == miParticipante.Id;
    }
}
