namespace RankingServicio.Commons.Dtos.Consultas;

public sealed record RankingGlobalDto(
    int Posicion,
    Guid ParticipanteIdentidadId,
    string Alias,
    long Puntaje);
