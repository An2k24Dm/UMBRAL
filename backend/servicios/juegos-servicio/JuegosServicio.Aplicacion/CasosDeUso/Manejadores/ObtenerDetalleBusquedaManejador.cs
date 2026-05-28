using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerDetalleBusquedaManejador
    : IRequestHandler<ObtenerDetalleBusquedaConsulta, BusquedaTesoroDetalleDto?>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ObtenerDetalleBusquedaManejador(IRepositorioBusquedas repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<BusquedaTesoroDetalleDto?> Handle(
        ObtenerDetalleBusquedaConsulta consulta, CancellationToken cancelacion)
    {
        return _repositorio.ObtenerDetalleBusquedaAsync(consulta.BusquedaId, cancelacion);
    }
}
