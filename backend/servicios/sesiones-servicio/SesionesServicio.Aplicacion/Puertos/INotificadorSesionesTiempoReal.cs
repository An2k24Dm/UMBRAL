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
}
