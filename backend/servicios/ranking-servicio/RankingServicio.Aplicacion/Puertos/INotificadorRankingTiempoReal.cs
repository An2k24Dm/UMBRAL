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

public sealed record PuntajeCalculadoDto(
    Guid EventoIdOrigen,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    long PuntajeGanado,
    long PuntajeTotalParticipante,
    long? PuntajeTotalEquipo,
    DateTime CalculadoEnUtc);

public sealed record PenalizacionAplicadaNotificacionDto(
    Guid SesionId,
    string TipoObjetivo,
    Guid ObjetivoId,
    int PuntosPenalizados,
    int PuntosPenalizadosAcumulados,
    long PuntajeResultante,
    DateTime AplicadaEnUtc);
