namespace SesionesServicio.Aplicacion.Puertos;

public interface INotificadorSesionesTiempoReal
{
    Task NotificarParticipantesSesionActualizadosAsync(
        Guid sesionId,
        CancellationToken cancelacion);

    Task NotificarEquiposSesionActualizadosAsync(
        Guid sesionId,
        Guid? equipoId,
        CancellationToken cancelacion);

    Task NotificarEquipoActualizadoAsync(
        Guid sesionId,
        Guid equipoId,
        CancellationToken cancelacion);

    Task NotificarSesionActualizadaAsync(
        Guid sesionId,
        string estado,
        CancellationToken cancelacion);

    Task NotificarParticipanteExpulsadoAsync(
        Guid participanteIdentidadId,
        Guid sesionId,
        Guid participanteSesionId,
        CancellationToken cancelacion);

    Task NotificarEquipoExpulsadoAsync(
        IReadOnlyCollection<Guid> participantesIdentidadIds,
        Guid sesionId,
        Guid equipoId,
        string equipoNombre,
        CancellationToken cancelacion);

    Task NotificarRespuestaRegistradaAsync(
        Guid sesionId,
        Guid etapaId,
        Guid preguntaId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        bool esCorrecta,
        int puntosGanados,
        CancellationToken cancelacion);

    Task NotificarEtapaCompletadaAsync(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        CancellationToken cancelacion);

    Task NotificarPistaLiberadaAsync(
        Guid sesionId,
        Guid etapaId,
        Guid? pistaId,
        string contenido,
        CancellationToken cancelacion);
}
