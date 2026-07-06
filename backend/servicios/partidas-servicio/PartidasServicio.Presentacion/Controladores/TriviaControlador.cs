using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Presentacion.Controladores;

[ApiController]
[Route("api/partidas")]
public sealed class TriviaControlador : ControllerBase
{
    private readonly IMediator _mediator;

    public TriviaControlador(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// HU-37: Jugar a una etapa de trivia.
    /// El participante envía su respuesta a una pregunta de trivia.
    /// </summary>
    [HttpPost("sesiones/{sesionId:guid}/misiones/{misionId:guid}/etapas/{etapaId:guid}/trivia/{triviaId:guid}/respuestas")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(RespuestaTriviaResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EnviarRespuesta(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid triviaId,
        [FromBody] EnviarRespuestaTriviaDto dto,
        CancellationToken cancelacion)
    {
        var comando = new EnviarRespuestaTriviaComando(sesionId, misionId, etapaId, triviaId, dto);
        var resultado = await _mediator.Send(comando, cancelacion);
        return Ok(resultado);
    }
}
