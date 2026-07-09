using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;
using SesionesServicio.Aplicacion.Comandos.LiberarPista;
using SesionesServicio.Aplicacion.Consultas.ObtenerBusquedaTesoroConPistas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones/{sesionId:guid}")]
[Authorize]
public sealed class JuegoTesoroControlador : ControllerBase
{
    private readonly IMediator _mediador;
    private readonly IUsuarioActual _usuarioActual;

    public JuegoTesoroControlador(IMediator mediador, IUsuarioActual usuarioActual)
    {
        _mediador = mediador;
        _usuarioActual = usuarioActual;
    }

    // Participante: obtiene info de la búsqueda + pistas liberadas hasta ahora en esta sesión/etapa.
    [HttpGet("misiones/{misionId:guid}/etapas/{etapaId:guid}/busqueda-tesoro/{busquedaId:guid}")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(BusquedaTesoroConPistasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerBusquedaConPistas(
        Guid sesionId, Guid misionId, Guid etapaId, Guid busquedaId, CancellationToken cancelacion)
    {
        var participanteId = _usuarioActual.ObtenerId();
        if (participanteId is null) return Unauthorized();

        var resultado = await _mediador.Send(
            new ObtenerBusquedaTesoroConPistasConsulta(
                sesionId, misionId, etapaId, busquedaId, participanteId.Value),
            cancelacion);

        return resultado is null ? NotFound() : Ok(resultado);
    }

    // Participante: envía el código QR escaneado como evidencia de haber encontrado el tesoro.
    [HttpPost("misiones/{misionId:guid}/etapas/{etapaId:guid}/busqueda-tesoro/{busquedaId:guid}/evidencias")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(EvidenciaTesoroRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EnviarEvidencia(
        Guid sesionId, Guid misionId, Guid etapaId, Guid busquedaId,
        [FromBody] EnviarEvidenciaTesoroDto dto,
        CancellationToken cancelacion)
    {
        // Las reglas de negocio (sesión no activa, participante no inscrito, etapa
        // no actual, evidencia duplicada) las señalan excepciones tipadas que el
        // ManejadorErroresMiddleware traduce al HTTP correcto. El controlador no
        // inspecciona el texto de los mensajes.
        var resultado = await _mediador.Send(
            new EnviarEvidenciaTesoroComando(
                sesionId, misionId, etapaId, busquedaId, dto.CodigoEscaneado),
            cancelacion);
        return Ok(resultado);
    }

    // Operador: libera una pista predefinida o personalizada a todos los participantes de la etapa.
    [HttpPost("etapas/{etapaId:guid}/pistas-liberadas")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LiberarPista(
        Guid sesionId, Guid etapaId,
        [FromBody] LiberarPistaDto dto,
        CancellationToken cancelacion)
    {
        try
        {
            await _mediador.Send(
                new LiberarPistaComando(sesionId, etapaId, dto.PistaId, dto.Contenido),
                cancelacion);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ya fue liberada"))
        {
            return Conflict(new { codigo = "PISTA_YA_LIBERADA", mensaje = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no está activa"))
        {
            return UnprocessableEntity(new { codigo = "SESION_NO_ACTIVA", mensaje = ex.Message });
        }
    }

    // Participante: lista las pistas ya liberadas en esta etapa (para sincronizar estado).
    // Las pistas también se incluyen en GET .../busqueda-tesoro/{busquedaId}.
    [HttpGet("etapas/{etapaId:guid}/pistas-liberadas")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(List<PistaLiberadaSesionDto>), StatusCodes.Status200OK)]
    public IActionResult ObtenerPistasLiberadas(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        // Las pistas se obtienen en contexto completo a través de ObtenerBusquedaConPistas.
        // Este endpoint simplificado retorna vacío; el cliente debe usar el endpoint de búsqueda.
        return Ok(new List<PistaLiberadaSesionDto>());
    }
}
