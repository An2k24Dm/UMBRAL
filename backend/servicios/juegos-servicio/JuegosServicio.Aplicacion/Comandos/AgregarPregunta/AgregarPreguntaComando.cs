using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.AgregarPregunta;

public sealed record AgregarPreguntaComando(Guid TriviaId, AgregarPreguntaDto Datos)
    : IRequest<Guid>;
