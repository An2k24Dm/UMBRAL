using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerMisionesEnBorradorManejador
    : IRequestHandler<ObtenerMisionesEnBorradorConsulta, List<MisionResumenDto>>
{
    private readonly IRepositorioMisiones _repositorio;

    public ObtenerMisionesEnBorradorManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<List<MisionResumenDto>> Handle(
        ObtenerMisionesEnBorradorConsulta consulta, CancellationToken cancelacion) =>
        _repositorio.ObtenerMisionesEnBorradorAsync(consulta.CreadorId, cancelacion);
}
