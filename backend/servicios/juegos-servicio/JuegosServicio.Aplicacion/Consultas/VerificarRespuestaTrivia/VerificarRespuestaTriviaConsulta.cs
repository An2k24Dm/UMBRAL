using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.VerificarRespuestaTrivia;

public sealed record VerificarRespuestaTriviaConsulta(
    Guid TriviaId,
    Guid PreguntaId,
    Guid OpcionSeleccionadaId)
    : IRequest<VerificacionRespuestaTriviaDto?>;
