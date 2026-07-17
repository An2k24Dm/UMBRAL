namespace RankingServicio.Commons.Dtos.Eventos.Salida;

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
