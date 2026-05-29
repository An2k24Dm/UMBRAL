using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerBusquedasActivasManejador
    : IRequestHandler<ObtenerBusquedasActivasConsulta, List<BusquedaTesoroResumenDto>>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ObtenerBusquedasActivasManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<List<BusquedaTesoroResumenDto>> Handle(
        ObtenerBusquedasActivasConsulta consulta, CancellationToken cancelacion)
    {
        return _repositorio.ObtenerBusquedasActivasAsync(cancelacion);
    }
}
