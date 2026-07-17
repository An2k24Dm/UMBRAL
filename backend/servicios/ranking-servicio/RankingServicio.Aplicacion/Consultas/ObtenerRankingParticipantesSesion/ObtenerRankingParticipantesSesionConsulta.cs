using MediatR;
using RankingServicio.Commons.Dtos.Consultas;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

public sealed record ObtenerRankingParticipantesSesionConsulta(Guid SesionId)
    : IRequest<List<RankingParticipanteDto>>;
