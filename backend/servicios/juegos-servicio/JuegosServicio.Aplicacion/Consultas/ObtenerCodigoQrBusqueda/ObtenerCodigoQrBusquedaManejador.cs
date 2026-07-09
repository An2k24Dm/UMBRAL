using JuegosServicio.Aplicacion.Puertos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerCodigoQrBusqueda;

public sealed class ObtenerCodigoQrBusquedaManejador : IRequestHandler<ObtenerCodigoQrBusquedaConsulta, string?>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ObtenerCodigoQrBusquedaManejador(IRepositorioBusquedas repositorio) =>
        _repositorio = repositorio;

    public Task<string?> Handle(ObtenerCodigoQrBusquedaConsulta consulta, CancellationToken cancelacion)
        => _repositorio.ObtenerCodigoQrAsync(consulta.BusquedaId, cancelacion);
}
