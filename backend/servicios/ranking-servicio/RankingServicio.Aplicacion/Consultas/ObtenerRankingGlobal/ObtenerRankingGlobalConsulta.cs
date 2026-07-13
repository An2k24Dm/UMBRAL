using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;

public sealed record ObtenerRankingGlobalConsulta(int Top)
    : IRequest<List<RankingGlobalDto>>;

public sealed record RankingGlobalDto(
    int Posicion,
    Guid ParticipanteIdentidadId,
    string Alias,
    long Puntaje);
