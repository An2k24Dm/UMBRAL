using MediatR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;

public sealed class ObtenerRankingEquiposSesionManejador
    : IRequestHandler<ObtenerRankingEquiposSesionConsulta, List<EntradaRankingEquipoDto>>
{
    private readonly IRepositorioRankingEquipo _repo;

    public ObtenerRankingEquiposSesionManejador(IRepositorioRankingEquipo repo)
    {
        _repo = repo;
    }

    public async Task<List<EntradaRankingEquipoDto>> Handle(
        ObtenerRankingEquiposSesionConsulta consulta, CancellationToken cancelacion)
    {
        var entradas = await _repo.ObtenerPorSesionAsync(consulta.SesionId, cancelacion);

        return entradas
            .OrderBy(e => e.Posicion)
            .Select(e => new EntradaRankingEquipoDto(
                e.Posicion,
                e.EquipoId,
                e.NombreEquipo,
                e.PuntajeTotal,
                e.EtapasCompletadas))
            .ToList();
    }
}
