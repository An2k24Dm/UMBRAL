using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;

public sealed record ObtenerRankingEquiposSesionConsulta(Guid SesionId)
    : IRequest<List<RankingEquipoDto>>;

// Posición calculada al consultar; nombre del equipo enriquecido desde
// sesiones-servicio. El detalle desplegable muestra el aporte de cada
// participante (derivado de RankingParticipante.EquipoId).
public sealed record RankingEquipoDto(
    int Posicion,
    Guid EquipoId,
    string NombreEquipo,
    long Puntaje,
    IReadOnlyList<AporteParticipanteEquipoDto> Participantes);

public sealed record AporteParticipanteEquipoDto(
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    string Alias,
    long Puntaje);
