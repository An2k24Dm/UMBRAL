using MediatR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;

public sealed class ObtenerRankingGlobalManejador
    : IRequestHandler<ObtenerRankingGlobalConsulta, List<EntradaRankingGlobalDto>>
{
    private readonly IRepositorioRankingGlobal _repo;

    public ObtenerRankingGlobalManejador(IRepositorioRankingGlobal repo)
    {
        _repo = repo;
    }

    public async Task<List<EntradaRankingGlobalDto>> Handle(
        ObtenerRankingGlobalConsulta consulta, CancellationToken cancelacion)
    {
        var entradas = await _repo.ObtenerTopAsync(consulta.Top, cancelacion);

        return entradas
            .Select((e, i) => new EntradaRankingGlobalDto(
                i + 1,
                e.ParticipanteIdentidadId,
                e.NombreParticipante,
                e.PuntajeAcumulado,
                e.SesionesJugadas,
                e.EtapasCompletadasTotal))
            .ToList();
    }
}
