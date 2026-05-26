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
