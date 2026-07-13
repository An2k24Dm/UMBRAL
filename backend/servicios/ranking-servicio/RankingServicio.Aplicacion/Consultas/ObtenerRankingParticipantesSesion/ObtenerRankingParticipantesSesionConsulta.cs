using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

public sealed record ObtenerRankingParticipantesSesionConsulta(Guid SesionId)
    : IRequest<List<RankingParticipanteDto>>;

// La posición se calcula al consultar (no se persiste). El alias se enriquece
// desde identidad-servicio. Se conservan identificadores necesarios para la
// presentación (equipo, identidad).
public sealed record RankingParticipanteDto(
    int Posicion,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    string Alias,
    long Puntaje);
