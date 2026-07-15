using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ActivarTrivia;

public sealed record ActivarTriviaComando(Guid TriviaId, Guid OperadorId) : IRequest;
