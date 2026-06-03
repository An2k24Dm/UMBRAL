using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarMisionComando(Guid MisionId) : IRequest;
