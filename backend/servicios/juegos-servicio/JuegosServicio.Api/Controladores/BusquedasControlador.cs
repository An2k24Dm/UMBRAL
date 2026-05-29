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

    // HU26 — Activar búsqueda del tesoro (Borrador → Activa).
    [HttpPatch("{busquedaId:guid}/activar")]
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

    // HU26 — Listar búsquedas activas (para selección en sesiones de juego).
    [HttpGet("activas")]
    [ProducesResponseType(typeof(List<BusquedaTesoroResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerBusquedasActivas(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerBusquedasActivasConsulta(), cancelacion);
        return Ok(resultado);
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

    // HU25 — Modificar una misión de una etapa (solo en estado Borrador).
    [HttpPut("{busquedaId:guid}/etapas/{etapaId:guid}/misiones/{misionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarMision(
        Guid busquedaId,
        Guid etapaId,
        Guid misionId,
        [FromBody] ModificarMisionDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarMisionComando(busquedaId, etapaId, misionId, dto), cancelacion);
        return NoContent();
    }

    // HU25 — Eliminar una misión de una etapa (solo en estado Borrador).
    [HttpDelete("{busquedaId:guid}/etapas/{etapaId:guid}/misiones/{misionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarMision(
        Guid busquedaId,
        Guid etapaId,
        Guid misionId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarMisionComando(busquedaId, etapaId, misionId), cancelacion);
        return NoContent();
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

    // HU24 — Modificar datos de una etapa (solo en estado Borrador).
    [HttpPut("{busquedaId:guid}/etapas/{etapaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarEtapa(
        Guid busquedaId,
        Guid etapaId,
        [FromBody] ModificarEtapaDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarEtapaComando(busquedaId, etapaId, dto), cancelacion);
        return NoContent();
    }

    // HU24 — Eliminar una etapa (solo en estado Borrador). Las misiones se borran en cascada.
    [HttpDelete("{busquedaId:guid}/etapas/{etapaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarEtapa(
        Guid busquedaId,
        Guid etapaId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarEtapaComando(busquedaId, etapaId), cancelacion);
        return NoContent();
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

    // HU21 — Listar búsquedas en borrador. Admin ve todas; Operador ve solo las suyas.
    [HttpGet("borrador")]
    [ProducesResponseType(typeof(List<BusquedaTesoroResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerBusquedasEnBorrador(CancellationToken cancelacion)
    {
        Guid? filtroCreador = User.IsInRole("Administrador") ? null : ObtenerCreadorId();
        var resultado = await _mediador.Send(
            new ObtenerBusquedasEnBorradorConsulta(filtroCreador), cancelacion);
        return Ok(resultado);
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
