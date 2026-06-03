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
[Authorize]
public sealed class BusquedasControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public BusquedasControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpPost]
    [Authorize(Policy = "PoliticaAdministrador")]
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

    [HttpGet("borrador")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(List<BusquedaTesoroResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerBusquedasEnBorrador(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerBusquedasEnBorradorConsulta(null), cancelacion);
        return Ok(resultado);
    }

    [HttpGet("activas")]
    [Authorize(Policy = "PoliticaOperador")]
    [ProducesResponseType(typeof(List<BusquedaTesoroResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerBusquedasActivas(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerBusquedasActivasConsulta(), cancelacion);
        return Ok(resultado);
    }

    [HttpGet("{busquedaId:guid}")]
    [Authorize(Policy = "PoliticaOperador")]
    [ProducesResponseType(typeof(BusquedaTesoroDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalleBusqueda(
        Guid busquedaId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerDetalleBusquedaConsulta(busquedaId), cancelacion);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPatch("{busquedaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarBusqueda(
        Guid busquedaId,
        [FromBody] ModificarBusquedaTesoroDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarBusquedaTesoroComando(busquedaId, dto), cancelacion);
        return NoContent();
    }

    [HttpPatch("{busquedaId:guid}/activar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivarBusqueda(Guid busquedaId, CancellationToken cancelacion)
    {
        await _mediador.Send(new ActivarBusquedaTesoroComando(busquedaId, ObtenerCreadorId()), cancelacion);
        return NoContent();
    }

    [HttpDelete("{busquedaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DesactivarBusqueda(Guid busquedaId, CancellationToken cancelacion)
    {
        await _mediador.Send(new DesactivarBusquedaTesoroComando(busquedaId, ObtenerCreadorId()), cancelacion);
        return NoContent();
    }

    [HttpDelete("{busquedaId:guid}/eliminar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarBusqueda(Guid busquedaId, CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarBusquedaTesoroComando(busquedaId), cancelacion);
        return NoContent();
    }

    // Pistas directamente bajo /busquedas/{id}/pistas (sin sub-recurso /mision).
    [HttpPost("{busquedaId:guid}/pistas")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AgregarPista(
        Guid busquedaId,
        [FromBody] AgregarPistaDto dto,
        CancellationToken cancelacion)
    {
        var pistaId = await _mediador.Send(new AgregarPistaComando(busquedaId, dto), cancelacion);
        return Created($"/api/juegos/busquedas/{busquedaId}/pistas/{pistaId}", new { id = pistaId });
    }

    [HttpPut("{busquedaId:guid}/pistas/{pistaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarPista(
        Guid busquedaId, Guid pistaId,
        [FromBody] ModificarPistaDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarPistaComando(busquedaId, pistaId, dto), cancelacion);
        return NoContent();
    }

    [HttpDelete("{busquedaId:guid}/pistas/{pistaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarPista(
        Guid busquedaId, Guid pistaId, CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarPistaComando(busquedaId, pistaId), cancelacion);
        return NoContent();
    }

    private Guid ObtenerCreadorId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var id)) return id;

        if (User.IsInRole("Administrador")) return Guid.Parse("11111111-1111-1111-1111-111111111111");

        throw new UnauthorizedAccessException("No se pudo determinar la identidad del operador.");
    }
}
