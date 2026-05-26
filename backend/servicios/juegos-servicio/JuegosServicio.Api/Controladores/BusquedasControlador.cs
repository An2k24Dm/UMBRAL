using System.Security.Claims;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Commons.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegosServicio.Api.Controladores;

[ApiController]
[Route("api/juegos/busquedas")]
[Authorize(Policy = "PoliticaOperador")]
public sealed class BusquedasControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public BusquedasControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // HU21 — Crear búsqueda del tesoro en estado Borrador.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CrearBusqueda(
        [FromBody] CrearBusquedaTesoroDto dto, CancellationToken cancelacion)
    {
        var creadorId = ObtenerCreadorId();
        var busquedaId = await _mediador.Send(new CrearBusquedaTesoroComando(dto, creadorId), cancelacion);
        return Created($"/api/juegos/busquedas/{busquedaId}", new { id = busquedaId });
    }

    // HU23 — Agregar misión a una etapa de la búsqueda del tesoro.
    [HttpPost("{busquedaId:guid}/etapas/{etapaId:guid}/misiones")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarMision(
        Guid busquedaId,
        Guid etapaId,
        [FromBody] AgregarMisionDto dto,
        CancellationToken cancelacion)
    {
        var misionId = await _mediador.Send(new AgregarMisionComando(busquedaId, etapaId, dto), cancelacion);
        return Created(
            $"/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}/misiones/{misionId}",
            new { id = misionId });
    }

    // HU22 — Agregar etapa a una búsqueda del tesoro.
    [HttpPost("{busquedaId:guid}/etapas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarEtapa(
        Guid busquedaId,
        [FromBody] AgregarEtapaDto dto,
        CancellationToken cancelacion)
    {
        var etapaId = await _mediador.Send(new AgregarEtapaComando(busquedaId, dto), cancelacion);
        return Created(
            $"/api/juegos/busquedas/{busquedaId}/etapas/{etapaId}",
            new { id = etapaId });
    }

    // HU22 — Obtener detalle de una búsqueda del tesoro con sus etapas y misiones.
    [HttpGet("{busquedaId:guid}")]
    [ProducesResponseType(typeof(BusquedaTesoroDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalleBusqueda(
        Guid busquedaId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerDetalleBusquedaConsulta(busquedaId), cancelacion);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    // HU21 — Listar búsquedas del tesoro en borrador del operador autenticado.
    [HttpGet("borrador")]
    [ProducesResponseType(typeof(List<BusquedaTesoroResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerBusquedasEnBorrador(CancellationToken cancelacion)
    {
        var operadorId = ObtenerCreadorId();
        var resultado = await _mediador.Send(
            new ObtenerBusquedasEnBorradorConsulta(operadorId), cancelacion);
        return Ok(resultado);
    }

    private Guid ObtenerCreadorId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var id)) return id;

        throw new UnauthorizedAccessException("No se pudo determinar la identidad del operador.");
    }
}
