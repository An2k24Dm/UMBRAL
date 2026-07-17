namespace RankingServicio.Commons.Dtos.Consultas;

public sealed record RankingParticipanteDto(
    int Posicion,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    string Alias,
    long Puntaje,
    int PuntosPenalizados);
