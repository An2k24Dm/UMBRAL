using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using SesionesServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;
using SesionesServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones/{sesionId:guid}")]
[Authorize]
public sealed class JuegoTriviaControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public JuegoTriviaControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpGet("misiones/{misionId:guid}/etapas/{etapaId:guid}/trivia/{triviaId:guid}")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(TriviaParticipanteJuegosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ObtenerTriviaParticipante(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid triviaId,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerTriviaParticipanteConsulta(sesionId, misionId, etapaId, triviaId),
            cancelacion);
        if (resultado is null)
            return NotFound(new { codigo = "TRIVIA_NO_ENCONTRADA", mensaje = "Trivia no encontrada." });
        return Ok(resultado);
    }

    [HttpPost("misiones/{misionId:guid}/etapas/{etapaId:guid}/trivia/{triviaId:guid}/respuestas")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(EnviarRespuestaTriviaRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnviarRespuesta(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid triviaId,
        [FromBody] EnviarRespuestaTriviaDto dto,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new EnviarRespuestaTriviaComando(
                sesionId, misionId, etapaId, triviaId,
                dto.PreguntaId, dto.OpcionSeleccionadaId,
                dto.TiempoTardadoMs),
            cancelacion);
        return Ok(resultado);
    }

    // Obtener las preguntas ya respondidas por el participante en una etapa.
    [HttpGet("misiones/{misionId:guid}/etapas/{etapaId:guid}/preguntas-respondidas")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPreguntasRespondidas(
        Guid sesionId,
        Guid etapaId,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerPreguntasRespondidasConsulta(sesionId, etapaId), cancelacion);
        return Ok(resultado);
    }
}
