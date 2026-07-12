using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;

public sealed record ObtenerRankingEquiposSesionConsulta(Guid SesionId)
    : IRequest<List<EntradaRankingEquipoDto>>;

public sealed record EntradaRankingEquipoDto(
    int Posicion,
    Guid EquipoId,
    string NombreEquipo,
    int PuntajeTotal,
    int EtapasCompletadas);
