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
}
