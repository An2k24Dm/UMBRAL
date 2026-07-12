using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

public sealed record ObtenerRankingParticipantesSesionConsulta(Guid SesionId)
    : IRequest<List<EntradaRankingParticipanteDto>>;

public sealed record EntradaRankingParticipanteDto(
    int Posicion,
    Guid ParticipanteIdentidadId,
    string NombreParticipante,
    int PuntajeTotal,
    int RespuestasCorrectas,
    int RespuestasTotales,
    int EtapasCompletadas);
