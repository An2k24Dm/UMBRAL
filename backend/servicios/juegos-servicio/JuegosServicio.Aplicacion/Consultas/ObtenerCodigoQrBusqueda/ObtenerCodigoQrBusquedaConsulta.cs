using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerCodigoQrBusqueda;

public sealed record ObtenerCodigoQrBusquedaConsulta(Guid BusquedaId) : IRequest<string?>;
