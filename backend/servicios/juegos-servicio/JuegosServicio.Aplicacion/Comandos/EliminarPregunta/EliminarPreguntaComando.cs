using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarPregunta;

public sealed record EliminarPreguntaComando(Guid TriviaId, Guid PreguntaId)
    : IRequest;
