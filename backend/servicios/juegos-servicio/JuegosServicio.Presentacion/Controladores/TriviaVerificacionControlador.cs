using JuegosServicio.Aplicacion.Puertos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegosServicio.Presentacion.Controladores;

/// <summary>
/// Endpoint consumido internamente por partidas-servicio para verificar respuestas.
/// Accesible a cualquier usuario autenticado (no solo Operador).
/// </summary>
[ApiController]
[Route("api/juegos/trivias")]
[Authorize]
public sealed class TriviaVerificacionControlador : ControllerBase
{
    private readonly IRepositorioJuegos _repositorio;

    public TriviaVerificacionControlador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    /// <summary>
    /// HU-37: Verifica si una opción es correcta para una pregunta de trivia.
    /// Devuelve puntajeBase y tiempoLimiteMs para que partidas-servicio calcule el puntaje.
    /// </summary>
    [HttpGet("{triviaId:guid}/preguntas/{preguntaId:guid}/verificar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerificarRespuesta(
        Guid triviaId,
        Guid preguntaId,
        [FromQuery] Guid opcionId,
        CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(triviaId, cancelacion);
        if (trivia is null)
            return NotFound(new { codigo = "TRIVIA_NO_ENCONTRADA", mensaje = "Trivia no encontrada." });

        var pregunta = trivia.Preguntas.FirstOrDefault(p => p.Id == preguntaId);
        if (pregunta is null)
            return NotFound(new { codigo = "PREGUNTA_NO_ENCONTRADA", mensaje = "Pregunta no encontrada." });

        var opcion = pregunta.Opciones.FirstOrDefault(o => o.Id == opcionId);
        if (opcion is null)
            return NotFound(new { codigo = "OPCION_NO_ENCONTRADA", mensaje = "Opción no encontrada." });

        return Ok(new
        {
            esCorrecta = opcion.EsCorrecta,
            puntajeBase = pregunta.PuntajeAsignado.Valor,
            tiempoLimiteMs = trivia.TiempoLimitePorPregunta.Valor
        });
    }
}
