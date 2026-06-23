using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarPregunta;

public sealed record ModificarPreguntaComando(Guid TriviaId, Guid PreguntaId, ModificarPreguntaDto Datos)
    : IRequest;
