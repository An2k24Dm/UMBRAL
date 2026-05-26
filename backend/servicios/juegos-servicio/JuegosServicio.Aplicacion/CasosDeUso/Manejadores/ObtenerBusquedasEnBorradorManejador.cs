using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerBusquedasEnBorradorManejador
    : IRequestHandler<ObtenerBusquedasEnBorradorConsulta, List<BusquedaTesoroResumenDto>>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ObtenerBusquedasEnBorradorManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<BusquedaTesoroResumenDto>> Handle(
        ObtenerBusquedasEnBorradorConsulta consulta, CancellationToken cancelacion)
    {
        return await _repositorio.ObtenerBusquedasEnBorradorAsync(consulta.CreadorId, cancelacion);
    }
}
