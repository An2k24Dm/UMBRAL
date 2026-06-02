using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarMisionComando(Guid BusquedaId) : IRequest;
