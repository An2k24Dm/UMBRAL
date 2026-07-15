using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarMision;

public sealed record EliminarMisionComando(Guid MisionId) : IRequest;
