using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarPista;

public sealed record EliminarPistaComando(Guid BusquedaId, Guid PistaId) : IRequest;
