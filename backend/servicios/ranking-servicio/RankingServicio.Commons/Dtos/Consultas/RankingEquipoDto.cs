namespace RankingServicio.Commons.Dtos.Consultas;

public sealed record RankingEquipoDto(
    int Posicion,
    Guid EquipoId,
    string NombreEquipo,
    long Puntaje,
    int PuntosPenalizados,
    IReadOnlyList<AporteParticipanteEquipoDto> Participantes);
