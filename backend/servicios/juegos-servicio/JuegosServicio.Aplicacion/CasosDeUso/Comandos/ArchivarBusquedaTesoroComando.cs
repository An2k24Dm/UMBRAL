using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ArchivarBusquedaTesoroComando(Guid BusquedaId, Guid OperadorId) : IRequest;
