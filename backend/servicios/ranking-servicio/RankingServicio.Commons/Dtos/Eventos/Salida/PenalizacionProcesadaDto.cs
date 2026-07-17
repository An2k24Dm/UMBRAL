namespace RankingServicio.Commons.Dtos.Eventos.Salida;

public sealed record PenalizacionProcesadaDto(
    Guid EventoIdOrigen,
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
