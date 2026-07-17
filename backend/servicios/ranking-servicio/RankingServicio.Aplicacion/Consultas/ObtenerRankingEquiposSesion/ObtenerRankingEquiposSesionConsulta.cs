using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;

public sealed record ObtenerRankingEquiposSesionConsulta(Guid SesionId)
    : IRequest<List<RankingEquipoDto>>;

public sealed record RankingEquipoDto(
    int Posicion,
    Guid EquipoId,
    string NombreEquipo,
    long Puntaje,
    int PuntosPenalizados,
    IReadOnlyList<AporteParticipanteEquipoDto> Participantes);

public sealed record AporteParticipanteEquipoDto(
    int Posicion,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    string Alias,
    long Puntaje);
