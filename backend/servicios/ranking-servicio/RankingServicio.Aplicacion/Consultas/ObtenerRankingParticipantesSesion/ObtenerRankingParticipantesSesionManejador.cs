using MediatR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

public sealed class ObtenerRankingParticipantesSesionManejador
    : IRequestHandler<ObtenerRankingParticipantesSesionConsulta, List<EntradaRankingParticipanteDto>>
{
    private readonly IRepositorioRankingParticipante _repo;

    public ObtenerRankingParticipantesSesionManejador(IRepositorioRankingParticipante repo)
    {
        _repo = repo;
    }

    public async Task<List<EntradaRankingParticipanteDto>> Handle(
        ObtenerRankingParticipantesSesionConsulta consulta, CancellationToken cancelacion)
    {
        var entradas = await _repo.ObtenerPorSesionAsync(consulta.SesionId, cancelacion);

        return entradas
            .OrderBy(e => e.Posicion)
            .Select(e => new EntradaRankingParticipanteDto(
                e.Posicion,
                e.ParticipanteIdentidadId,
                e.NombreParticipante,
                e.PuntajeTotal,
                e.RespuestasCorrectas,
                e.RespuestasTotales,
                e.EtapasCompletadas))
            .ToList();
    }
}
