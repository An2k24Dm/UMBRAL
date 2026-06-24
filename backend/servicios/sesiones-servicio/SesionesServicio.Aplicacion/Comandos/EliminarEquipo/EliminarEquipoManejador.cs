using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.EliminarEquipo;

// HU42 — Eliminar un equipo de una sesión grupal En Preparación. Solo el
// líder puede hacerlo. El dominio protege las invariantes; aquí se orquesta
// la autorización por rol y la persistencia.
public sealed class EliminarEquipoManejador : IRequestHandler<EliminarEquipoComando>
{
    private const string RolParticipante = "Participante";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;

    public EliminarEquipoManejador(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        INotificadorSesionesTiempoReal notificadorTiempoReal)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _notificadorTiempoReal = notificadorTiempoReal;
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
    }
}
