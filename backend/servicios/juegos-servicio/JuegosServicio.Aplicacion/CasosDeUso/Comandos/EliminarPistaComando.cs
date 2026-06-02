using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarPistaComando(Guid BusquedaId, Guid PistaId) : IRequest;
