using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.AbandonarSesion;

public sealed class AbandonarSesionManejador : IRequestHandler<AbandonarSesionComando>
{
    private const string RolParticipante = "Participante";

    private readonly IValidador<AbandonarSesionComando> _validador;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public AbandonarSesionManejador(
        IValidador<AbandonarSesionComando> validador,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        INotificadorSesionesTiempoReal notificadorTiempoReal,
        IRegistroLogsAplicacion registroLogs)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _notificadorTiempoReal = notificadorTiempoReal;
        _registroLogs = registroLogs;
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
            // Refresca el conteo de inscritos en el listado (web y móvil).
            await _notificadorTiempoReal.NotificarSesionActualizadaAsync(
                individual.Id, individual.Estado.ToString(), cancelacion);

            _registroLogs.Informacion(
                evento: "SesionAbandonada",
                descripcion: "Participante abandonó la sesión correctamente",
                propiedades: new Dictionary<string, object?>
                {
                    ["SesionId"] = individual.Id,
                    ["ParticipanteId"] = participanteId
                });
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

        var equipoSigueExistiendo = grupal.Equipos.Any(e => e.Id == equipoId);
        if (equipoSigueExistiendo)
            await _notificadorTiempoReal.NotificarEquipoActualizadoAsync(
                grupal.Id, equipoId, cancelacion);
        // Refresca el conteo de equipos/participantes en el listado.
        await _notificadorTiempoReal.NotificarSesionActualizadaAsync(
            grupal.Id, grupal.Estado.ToString(), cancelacion);

        _registroLogs.Informacion(
            evento: "EquipoAbandonado",
            descripcion: "Participante abandonó el equipo correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = grupal.Id,
                ["ParticipanteId"] = participanteId,
                ["EquipoId"] = equipoId
            });
    }
}
