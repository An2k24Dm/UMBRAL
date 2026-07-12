using MediatR;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;

public sealed record ObtenerRankingGlobalConsulta(int Top = 20)
    : IRequest<List<EntradaRankingGlobalDto>>;

public sealed record EntradaRankingGlobalDto(
    int Posicion,
    Guid ParticipanteIdentidadId,
    string NombreParticipante,
    long PuntajeAcumulado,
    int SesionesJugadas,
    int EtapasCompletadasTotal);
