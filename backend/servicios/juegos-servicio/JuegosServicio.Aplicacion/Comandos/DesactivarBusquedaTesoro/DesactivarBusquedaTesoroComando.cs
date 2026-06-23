using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.DesactivarBusquedaTesoro;

public sealed record DesactivarBusquedaTesoroComando(Guid BusquedaId, Guid OperadorId) : IRequest;
