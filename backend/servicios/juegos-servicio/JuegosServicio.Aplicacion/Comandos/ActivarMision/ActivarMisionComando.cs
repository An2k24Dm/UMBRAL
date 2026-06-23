using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ActivarMision;

public sealed record ActivarMisionComando(Guid MisionId) : IRequest;
