using MediatR;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Consultas.ObtenerRankingSesion;

public sealed record ObtenerRankingSesionConsulta(Guid SesionId)
    : IRequest<IReadOnlyList<RankingEntradaDto>>;
