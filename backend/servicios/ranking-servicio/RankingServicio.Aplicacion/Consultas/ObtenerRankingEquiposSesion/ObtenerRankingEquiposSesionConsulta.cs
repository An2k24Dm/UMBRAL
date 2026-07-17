using MediatR;
using RankingServicio.Commons.Dtos.Consultas;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;

public sealed record ObtenerRankingEquiposSesionConsulta(Guid SesionId)
    : IRequest<List<RankingEquipoDto>>;
