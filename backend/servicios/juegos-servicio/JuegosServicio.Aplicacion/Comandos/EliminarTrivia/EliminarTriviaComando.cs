using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarTrivia;

public sealed record EliminarTriviaComando(Guid TriviaId) : IRequest;
