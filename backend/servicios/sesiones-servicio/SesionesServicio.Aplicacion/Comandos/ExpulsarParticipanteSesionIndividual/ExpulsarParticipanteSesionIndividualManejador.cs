using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteSesionIndividual;

public sealed class ExpulsarParticipanteSesionIndividualManejador
    : IRequestHandler<ExpulsarParticipanteSesionIndividualComando>
{
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificadorTiempoReal;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ExpulsarParticipanteSesionIndividualManejador(
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

    public async Task Handle(
        ExpulsarParticipanteSesionIndividualComando comando,
        CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para expulsar participantes.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Operador puede expulsar participantes o equipos.");

        var operadorId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del operador.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionIndividual individual)
            throw new SesionInvalidaExcepcion(
                "Solo se pueden expulsar participantes de sesiones individuales.");

        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el Operador creador de la sesión puede expulsar participantes o equipos.");

        var participanteIdentidadId = individual.Participantes
            .FirstOrDefault(p => p.Id == comando.ParticipanteSesionId)
            ?.ParticipanteIdentidadId;

        individual.ExpulsarParticipante(comando.ParticipanteSesionId);

        await _repositorio.ActualizarAsync(individual, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificadorTiempoReal.NotificarParticipantesSesionActualizadosAsync(
            individual.Id, cancelacion);
        if (participanteIdentidadId is Guid identidadId)
            await _notificadorTiempoReal.NotificarParticipanteExpulsadoAsync(
                identidadId, individual.Id, comando.ParticipanteSesionId, cancelacion);

        _registroLogs.Informacion(
            evento: "ParticipanteExpulsadoSesionIndividual",
            descripcion: "Operador expulsó un participante de una sesión individual correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = individual.Id,
                ["ParticipanteSesionId"] = comando.ParticipanteSesionId,
                ["OperadorId"] = operadorId
            });
    }
}
