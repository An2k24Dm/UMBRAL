using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.AbandonarSesion;

// HU48 — Abandono voluntario. Solo el propio Participante (Administrador y
// Operador no pueden usar esta acción) y solo con la sesión En Preparación.
// En sesión individual elimina su participación; en sesión grupal lo saca de
// su equipo (reasignando liderazgo, o eliminando el equipo si quedó vacío).
// El dominio protege las reglas; aquí se orquesta autorización, persistencia
// y notificación en tiempo real (solo eventos generales, nunca el aviso de
// expulsado: fue un abandono voluntario, y siempre después de guardar).
public sealed class AbandonarSesionManejador : IRequestHandler<AbandonarSesionComando>
{
    private const string RolParticipante = "Participante";

    private readonly IValidador<AbandonarSesionComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;

    public AbandonarSesionManejador(
        IValidador<AbandonarSesionComando> validador,
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

    public async Task Handle(AbandonarSesionComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para abandonar una sesión.");

        if (!_usuarioActual.TieneAlgunRol(RolParticipante))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Participante puede abandonar una sesión o un equipo.");

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del participante.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is SesionIndividual individual)
        {
            individual.AbandonarSesion(participanteId);

            await _repositorio.ActualizarAsync(individual, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
            await _notificadorTiempoReal.NotificarParticipantesSesionActualizadosAsync(
                individual.Id, cancelacion);
            return;
        }

        if (sesion is not SesionGrupal grupal)
            throw new SesionInvalidaExcepcion("El modo de la sesión no es válido.");

        var participanteRemovido = grupal.AbandonarEquipo(participanteId);

        var equipoId = participanteRemovido.EquipoId
            ?? throw new SesionInvalidaExcepcion(
                "No se pudo determinar el equipo abandonado.");

        await _repositorio.ActualizarAsync(grupal, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificadorTiempoReal.NotificarEquiposSesionActualizadosAsync(
            grupal.Id, equipoId, cancelacion);

        // Si el equipo quedó vacío fue eliminado del agregado: no se emite
        // EquipoActualizado para no llevar a los clientes a un detalle
        // inexistente; con EquiposSesionActualizados basta para los listados.
        var equipoSigueExistiendo = grupal.Equipos.Any(e => e.Id == equipoId);
        if (equipoSigueExistiendo)
            await _notificadorTiempoReal.NotificarEquipoActualizadoAsync(
                grupal.Id, equipoId, cancelacion);
    }
}
