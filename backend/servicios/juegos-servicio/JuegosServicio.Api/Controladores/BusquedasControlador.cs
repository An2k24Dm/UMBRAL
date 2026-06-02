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

    // HU21 — Crear búsqueda del tesoro en estado Inactiva.
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

    // HU21 — Listar búsquedas inactivas (gestión interna del catálogo).
    [HttpGet("borrador")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(List<BusquedaTesoroResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerBusquedasEnBorrador(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerBusquedasEnBorradorConsulta(null), cancelacion);
        return Ok(resultado);
    }

    // HU26 / HU34 — Listar búsquedas activas (para selección en sesiones de juego).
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

    // HU22 / HU34 — Obtener detalle de una búsqueda con su misión y pistas.
    [HttpGet("{busquedaId:guid}")]
    [Authorize(Policy = "PoliticaOperador")]
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

    // HU26 — Activar búsqueda del tesoro (Inactiva → Activa). Requiere misión asignada.
    [HttpPatch("{busquedaId:guid}/activar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivarBusqueda(Guid busquedaId, CancellationToken cancelacion)
    {
        var operadorId = ObtenerCreadorId();
        await _mediador.Send(new ActivarBusquedaTesoroComando(busquedaId, operadorId), cancelacion);
        return NoContent();
    }

    // HU26 — Desactivar búsqueda del tesoro (Activa → Inactiva).
    [HttpDelete("{busquedaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DesactivarBusqueda(Guid busquedaId, CancellationToken cancelacion)
    {
        var operadorId = ObtenerCreadorId();
        await _mediador.Send(new DesactivarBusquedaTesoroComando(busquedaId, operadorId), cancelacion);
        return NoContent();
    }

    // Eliminar búsqueda del tesoro de la base de datos (solo si está Inactiva).
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

    // HU23 — Asignar la misión única a una búsqueda (solo en estado Inactiva).
    [HttpPost("{busquedaId:guid}/mision")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AsignarMision(
        Guid busquedaId,
        [FromBody] AgregarMisionDto dto,
        CancellationToken cancelacion)
    {
        var misionId = await _mediador.Send(new AgregarMisionComando(busquedaId, dto), cancelacion);
        return Created($"/api/juegos/busquedas/{busquedaId}/mision", new { id = misionId });
    }

    // HU25 — Modificar la misión de una búsqueda (solo en estado Inactiva).
    [HttpPut("{busquedaId:guid}/mision")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarMision(
        Guid busquedaId,
        [FromBody] ModificarMisionDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarMisionComando(busquedaId, dto), cancelacion);
        return NoContent();
    }

    // HU25 — Eliminar la misión de una búsqueda (solo en estado Inactiva).
    [HttpDelete("{busquedaId:guid}/mision")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarMision(
        Guid busquedaId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarMisionComando(busquedaId), cancelacion);
        return NoContent();
    }

    // HU28 — Agregar pista de ayuda a la misión (se puede hacer en cualquier estado para liberarla en tiempo real).
    [HttpPost("{busquedaId:guid}/mision/pistas")]
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
        return Created(
            $"/api/juegos/busquedas/{busquedaId}/mision/pistas/{pistaId}",
            new { id = pistaId });
    }

    // HU30 — Modificar el contenido de una pista (solo en estado Inactiva).
    [HttpPut("{busquedaId:guid}/mision/pistas/{pistaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarPista(
        Guid busquedaId,
        Guid pistaId,
        [FromBody] ModificarPistaDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarPistaComando(busquedaId, pistaId, dto), cancelacion);
        return NoContent();
    }

    // HU32 — Eliminar una pista (solo en estado Inactiva).
    [HttpDelete("{busquedaId:guid}/mision/pistas/{pistaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarPista(
        Guid busquedaId,
        Guid pistaId,
        CancellationToken cancelacion)
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
