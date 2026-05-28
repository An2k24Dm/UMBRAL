using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record DesactivarTriviaComando(Guid TriviaId, Guid OperadorId) : IRequest;
