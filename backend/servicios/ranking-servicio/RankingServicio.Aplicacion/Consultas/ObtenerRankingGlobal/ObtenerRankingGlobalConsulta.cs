using MediatR;
using RankingServicio.Commons.Dtos.Consultas;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;

public sealed record ObtenerRankingGlobalConsulta(int Top)
    : IRequest<List<RankingGlobalDto>>;
