using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ModificarPreguntaComando(Guid TriviaId, Guid PreguntaId, ModificarPreguntaDto Datos)
    : IRequest;
