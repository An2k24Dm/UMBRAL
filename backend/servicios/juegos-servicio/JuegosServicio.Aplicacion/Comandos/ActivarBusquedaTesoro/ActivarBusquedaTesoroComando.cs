using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ActivarBusquedaTesoro;

public sealed record ActivarBusquedaTesoroComando(Guid BusquedaId, Guid OperadorId) : IRequest;
