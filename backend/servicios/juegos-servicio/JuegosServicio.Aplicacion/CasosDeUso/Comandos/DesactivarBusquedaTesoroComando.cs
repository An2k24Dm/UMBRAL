using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record DesactivarBusquedaTesoroComando(Guid BusquedaId, Guid OperadorId) : IRequest;
