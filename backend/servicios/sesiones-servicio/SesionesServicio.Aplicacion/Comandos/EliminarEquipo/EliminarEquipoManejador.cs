using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.EliminarEquipo;

public sealed class EliminarEquipoManejador : IRequestHandler<EliminarEquipoComando>
{
    private const string RolParticipante = "Participante";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarEquipoManejador(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        INotificadorSesionesTiempoReal notificadorTiempoReal,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _notificadorTiempoReal = notificadorTiempoReal;
        _registroLogs = registroLogs;
    }

    public async Task Handle(EliminarEquipoComando comando, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para eliminar un equipo.");

        if (!_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante puede eliminar equipos.");

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionGrupal sesionGrupal)
            throw new SesionNoGrupalExcepcion(
                "Solo se pueden eliminar equipos de sesiones grupales.");

        sesionGrupal.EliminarEquipo(comando.EquipoId, participanteId);

        await _repositorio.ActualizarAsync(sesionGrupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            sesionGrupal.Id, comando.EquipoId, cancelacion);

        _registroLogs.Informacion(
            evento: "EquipoEliminado",
            descripcion: "Participante eliminó un equipo correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesionGrupal.Id,
                ["EquipoId"] = comando.EquipoId,
                ["ParticipanteId"] = participanteId
            });
    }
}
