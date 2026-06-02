using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarBusquedaTesoroComando(Guid BusquedaId) : IRequest;
