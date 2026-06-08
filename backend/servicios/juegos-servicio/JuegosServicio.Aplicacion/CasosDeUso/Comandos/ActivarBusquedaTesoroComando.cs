using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ActivarBusquedaTesoroComando(Guid BusquedaId, Guid OperadorId) : IRequest;
