namespace RankingServicio.Aplicacion.Puertos;

public interface IPublicadorResultadosPuntaje
{
    Task PublicarPuntajeActualizadoAsync(
        PuntajeCalculadoDto puntaje, CancellationToken cancelacion);

    // HU52 — Resultado explícito de penalización procesada (routing key
    // ranking.penalizacion_procesada). No se sobrecarga ranking.puntaje_actualizado
    // porque su contrato exige siempre un participante.
    Task PublicarPenalizacionProcesadaAsync(
        PenalizacionProcesadaDto penalizacion, CancellationToken cancelacion);
}

// HU52 — Resultado de Ranking hacia Sesiones tras aplicar una penalización.
public sealed record PenalizacionProcesadaDto(
    Guid EventoIdOrigen,
    Guid PenalizacionId,
    Guid SesionId,
    string TipoObjetivo,
    Guid? ParticipanteSesionId,
    Guid? ParticipanteIdentidadId,
    Guid? EquipoId,
    int PuntosPenalizados,
    int PuntosPenalizadosAcumulados,
    long? PuntajeTotalParticipante,
    long? PuntajeTotalEquipo,
    DateTime CalculadoEnUtc);
