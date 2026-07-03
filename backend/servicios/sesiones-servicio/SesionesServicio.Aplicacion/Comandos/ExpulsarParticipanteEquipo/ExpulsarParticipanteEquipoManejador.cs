using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteEquipo;

// HU45 — Expulsar a un participante de un equipo. El líder del equipo puede
// expulsar integrantes normales; el Operador dueño de la sesión puede
// expulsar a cualquiera (si expulsa al líder, el dominio reasigna el
// liderazgo). El Administrador no puede expulsar. El dominio protege las
// reglas de estado/pertenencia; aquí se orquesta autorización, persistencia
// y notificación en tiempo real (siempre después de guardar).
public sealed class ExpulsarParticipanteEquipoManejador
    : IRequestHandler<ExpulsarParticipanteEquipoComando>
{
    private const string RolOperador = "Operador";
    private const string RolParticipante = "Participante";

    private readonly IValidador<ExpulsarParticipanteEquipoComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;

    public ExpulsarParticipanteEquipoManejador(
        IValidador<ExpulsarParticipanteEquipoComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        INotificadorSesionesTiempoReal notificadorTiempoReal)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _notificadorTiempoReal = notificadorTiempoReal;
    }

    public async Task Handle(
        ExpulsarParticipanteEquipoComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para expulsar participantes.");

        var esOperador = _usuarioActual.TieneAlgunRol(RolOperador);
        var esParticipante = _usuarioActual.TieneAlgunRol(RolParticipante);
        if (!esOperador && !esParticipante)
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Operador o un Participante pueden expulsar participantes de un equipo.");

        var actorId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del usuario.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionGrupal sesionGrupal)
            throw new SesionNoGrupalExcepcion(
                "Solo se pueden expulsar participantes de equipos en sesiones grupales.");

        // El Operador solo actúa sobre sesiones creadas por él.
        if (esOperador && sesionGrupal.OperadorCreadorId != actorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el Operador creador de la sesión puede expulsar participantes de sus equipos.");

        var expulsado = sesionGrupal.ExpulsarParticipanteDeEquipo(
            comando.EquipoId,
            comando.ParticipanteSesionId,
            actorId,
            actorEsOperador: esOperador);

        await _repositorio.ActualizarAsync(sesionGrupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        // SignalR solo notifica que algo cambió, y siempre después de guardar.
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            sesionGrupal.Id, comando.EquipoId, cancelacion);
        await _notificadorTiempoReal.NotificarEquipoActualizadoAsync(
            sesionGrupal.Id, comando.EquipoId, cancelacion);
        // Aviso dirigido: el expulsado queda fuera de la sesión grupal.
        await _notificadorTiempoReal.NotificarParticipanteExpulsadoAsync(
            expulsado.ParticipanteIdentidadId,
            sesionGrupal.Id,
            expulsado.Id,
            cancelacion);
    }
}
