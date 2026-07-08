using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using SesionesServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones/{sesionId:guid}")]
[Authorize]
public sealed class JuegoTriviaControlador : ControllerBase
{
    private readonly IMediator _mediador;
    private readonly IClienteJuegosTrivia _clienteTrivia;

    public JuegoTriviaControlador(IMediator mediador, IClienteJuegosTrivia clienteTrivia)
    {
        _mediador = mediador;
        _clienteTrivia = clienteTrivia;
    }

    // Obtener la trivia sin respuestas correctas para el participante.
    [HttpGet("trivia/{triviaId:guid}")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(TriviaParticipanteJuegosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerTriviaParticipante(
        Guid sesionId,
        Guid triviaId,
        CancellationToken cancelacion)
    {
        var resultado = await _clienteTrivia.ObtenerTriviaParticipanteAsync(triviaId, cancelacion);
        if (resultado is null)
            return NotFound(new { codigo = "TRIVIA_NO_ENCONTRADA", mensaje = "Trivia no encontrada." });
        return Ok(resultado);
    }

    // Enviar respuesta a una pregunta de trivia durante el juego.
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
        try
        {
            var resultado = await _mediador.Send(
                new EnviarRespuestaTriviaComando(
                    sesionId, misionId, etapaId, triviaId,
                    dto.PreguntaId, dto.OpcionSeleccionadaId,
                    dto.TiempoTardadoMs, dto.TotalPreguntasEtapa),
                cancelacion);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Ya respondiste"))
        {
            return Conflict(new { codigo = "YA_RESPONDIDA", mensaje = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no está activa"))
        {
            return UnprocessableEntity(new { codigo = "SESION_NO_ACTIVA", mensaje = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no está inscrito"))
        {
            return Forbid();
        }
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
