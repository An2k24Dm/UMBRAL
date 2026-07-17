using RankingServicio.Commons.Dtos.Eventos.Salida;
using RankingServicio.Commons.Dtos.TiempoReal;

namespace RankingServicio.Aplicacion.Puertos;

public interface INotificadorRankingTiempoReal
{
    Task NotificarPuntajeCalculadoAsync(
        PuntajeCalculadoDto puntaje, CancellationToken cancelacion);

    Task NotificarRankingParticipantesActualizadoAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task NotificarRankingEquiposActualizadoAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task NotificarPenalizacionAplicadaAsync(
        PenalizacionAplicadaNotificacionDto penalizacion, CancellationToken cancelacion);
}
