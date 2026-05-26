using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ActivarTriviaComando(Guid TriviaId, Guid OperadorId) : IRequest;
