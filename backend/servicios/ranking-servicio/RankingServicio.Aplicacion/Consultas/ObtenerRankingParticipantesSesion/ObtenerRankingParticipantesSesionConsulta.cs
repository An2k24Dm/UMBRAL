using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

public sealed record ObtenerRankingParticipantesSesionConsulta(Guid SesionId)
    : IRequest<List<RankingParticipanteDto>>;

public sealed record RankingParticipanteDto(
    int Posicion,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    string Alias,
    long Puntaje,
    int PuntosPenalizados);
