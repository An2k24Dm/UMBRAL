using MediatR;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Consultas.ObtenerRankingSesion;

public sealed class ObtenerRankingSesionManejador
    : IRequestHandler<ObtenerRankingSesionConsulta, IReadOnlyList<RankingEntradaDto>>
{
    private readonly IConsultasPartidas _consultas;

    public ObtenerRankingSesionManejador(IConsultasPartidas consultas)
    {
        _consultas = consultas;
    }

    public Task<IReadOnlyList<RankingEntradaDto>> Handle(
        ObtenerRankingSesionConsulta consulta, CancellationToken cancelacion)
        => _consultas.ObtenerRankingAsync(consulta.SesionId, cancelacion);
}
