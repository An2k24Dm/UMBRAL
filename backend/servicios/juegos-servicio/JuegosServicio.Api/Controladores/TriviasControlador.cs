using System.Security.Claims;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Commons.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegosServicio.Api.Controladores;

[ApiController]
[Route("api/juegos/trivias")]
[Authorize(Policy = "PoliticaOperador")]
public sealed class TriviasControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public TriviasControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // HU15 — Crear trivia (cascarón inicial en estado Borrador).
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CrearTrivia(
        [FromBody] CrearTriviaDto dto, CancellationToken cancelacion)
    {
        var creadorId = ObtenerCreadorId();
        var triviaId = await _mediador.Send(new CrearTriviaComando(dto, creadorId), cancelacion);
        return Created($"/api/juegos/trivias/{triviaId}", new { id = triviaId });
    }

    // HU15 — Detalle completo de una trivia (con preguntas y opciones).
    [HttpGet("{triviaId:guid}")]
    [ProducesResponseType(typeof(TriviaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(Guid triviaId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerDetalleTriviaConsulta(triviaId), cancelacion);
        if (resultado is null) return NotFound(new { mensaje = "Trivia no encontrada." });
        return Ok(resultado);
    }

    // HU15 — Listar trivias en borrador del operador autenticado.
    [HttpGet("borrador")]
    [ProducesResponseType(typeof(List<TriviaResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerTriviasEnBorrador(CancellationToken cancelacion)
    {
        var operadorId = ObtenerCreadorId();
        var resultado = await _mediador.Send(
            new ObtenerTriviasEnBorradorConsulta(operadorId), cancelacion);
        return Ok(resultado);
    }

    // HU16 — Agregar pregunta a una trivia en borrador.
    [HttpPost("{triviaId:guid}/preguntas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarPregunta(
        Guid triviaId,
        [FromBody] AgregarPreguntaDto dto,
        CancellationToken cancelacion)
    {
        var preguntaId = await _mediador.Send(
            new AgregarPreguntaComando(triviaId, dto), cancelacion);
        return Created(
            $"/api/juegos/trivias/{triviaId}/preguntas/{preguntaId}",
            new { id = preguntaId });
    }

    private Guid ObtenerCreadorId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var id)) return id;

        throw new UnauthorizedAccessException("No se pudo determinar la identidad del operador.");
    }
}
