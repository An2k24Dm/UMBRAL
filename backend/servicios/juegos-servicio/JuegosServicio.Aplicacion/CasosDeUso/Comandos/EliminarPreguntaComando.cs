using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarPreguntaComando(Guid TriviaId, Guid PreguntaId)
    : IRequest;
