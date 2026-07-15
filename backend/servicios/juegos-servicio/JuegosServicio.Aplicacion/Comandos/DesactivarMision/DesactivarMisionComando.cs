using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.DesactivarMision;

public sealed record DesactivarMisionComando(Guid MisionId) : IRequest;
