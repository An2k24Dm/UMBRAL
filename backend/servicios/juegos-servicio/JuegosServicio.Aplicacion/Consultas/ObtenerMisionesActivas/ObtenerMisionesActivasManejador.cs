using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerMisionesActivas;

public sealed class ObtenerMisionesActivasManejador
    : IRequestHandler<ObtenerMisionesActivasConsulta, List<MisionResumenDto>>
{
    private readonly IRepositorioMisiones _repositorio;

    public ObtenerMisionesActivasManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<List<MisionResumenDto>> Handle(
        ObtenerMisionesActivasConsulta consulta, CancellationToken cancelacion) =>
        _repositorio.ObtenerMisionesActivasAsync(cancelacion);
}
