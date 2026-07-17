namespace RankingServicio.Commons.Dtos.Consultas;

public sealed record AporteParticipanteEquipoDto(
    int Posicion,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    string Alias,
    long Puntaje);
