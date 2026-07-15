using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarBusquedaTesoro;

public sealed record EliminarBusquedaTesoroComando(Guid BusquedaId) : IRequest;
