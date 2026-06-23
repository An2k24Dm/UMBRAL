using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.DesactivarTrivia;

public sealed record DesactivarTriviaComando(Guid TriviaId, Guid OperadorId) : IRequest;
