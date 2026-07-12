namespace RankingServicio.Aplicacion.Puertos;

public interface INotificadorRankingTiempoReal
{
    Task NotificarRankingParticipantesActualizadoAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task NotificarRankingEquiposActualizadoAsync(
        Guid sesionId, CancellationToken cancelacion);
}
