using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarTriviaComando(Guid TriviaId) : IRequest;
