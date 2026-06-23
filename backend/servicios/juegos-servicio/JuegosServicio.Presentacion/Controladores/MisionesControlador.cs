using System.Security.Claims;
using JuegosServicio.Aplicacion.Comandos.ActivarMision;
using JuegosServicio.Aplicacion.Comandos.AgregarEtapa;
using JuegosServicio.Aplicacion.Comandos.CrearMision;
using JuegosServicio.Aplicacion.Comandos.DesactivarMision;
using JuegosServicio.Aplicacion.Comandos.EliminarEtapa;
using JuegosServicio.Aplicacion.Comandos.EliminarMision;
using JuegosServicio.Aplicacion.Comandos.ModificarMision;
using JuegosServicio.Aplicacion.Consultas.ObtenerDetalleMision;
using JuegosServicio.Aplicacion.Consultas.ObtenerDetalleMisionParticipante;
using JuegosServicio.Aplicacion.Consultas.ObtenerMisionesActivas;
using JuegosServicio.Aplicacion.Consultas.ObtenerMisionesEnBorrador;
using JuegosServicio.Commons.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegosServicio.Presentacion.Controladores;

[ApiController]
[Route("api/juegos/misiones")]
[Authorize]
public sealed class MisionesControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public MisionesControlador(IMediator mediador)
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
    public async Task<IActionResult> CrearMision(
        [FromBody] CrearMisionDto dto, CancellationToken cancelacion)
    {
        var creadorId = ObtenerCreadorId();
        var misionId = await _mediador.Send(new CrearMisionComando(dto, creadorId), cancelacion);
        return Created($"/api/juegos/misiones/{misionId}", new { id = misionId });
    }

    [HttpGet("borrador")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(List<MisionResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerMisionesEnBorrador(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerMisionesEnBorradorConsulta(null), cancelacion);
        return Ok(resultado);
    }

    [HttpGet("activas")]
    [Authorize(Policy = "PoliticaOperador")]
    [ProducesResponseType(typeof(List<MisionResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerMisionesActivas(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerMisionesActivasConsulta(), cancelacion);
        return Ok(resultado);
    }

    [HttpGet("{misionId:guid}")]
    [Authorize(Policy = "PoliticaOperador")]
    [ProducesResponseType(typeof(MisionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalleMision(
        Guid misionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerDetalleMisionConsulta(misionId), cancelacion);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    // Endpoint específico para el flujo móvil del Participante.
    // Devuelve un DTO recortado (sin creadorId ni fechas internas) y
    // solo si la misión está Activa. Los borradores nunca se exponen
    // al Participante, aún si una sesión los referencia.
    //
    // Se mantiene separado del endpoint administrativo
    // (GET /api/juegos/misiones/{misionId}) para preservar el principio
    // de menor privilegio: el Operador/Administrador conserva su
    // endpoint con datos completos; el Participante usa este.
    [HttpGet("participante/{misionId:guid}")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(MisionDetalleParticipanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalleMisionParticipante(
        Guid misionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerDetalleMisionParticipanteConsulta(misionId), cancelacion);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPatch("{misionId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ModificarMision(
        Guid misionId,
        [FromBody] ModificarMisionDto dto,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new ModificarMisionComando(misionId, dto), cancelacion);
        return NoContent();
    }

    [HttpPatch("{misionId:guid}/activar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivarMision(Guid misionId, CancellationToken cancelacion)
    {
        await _mediador.Send(new ActivarMisionComando(misionId), cancelacion);
        return NoContent();
    }

    [HttpDelete("{misionId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DesactivarMision(Guid misionId, CancellationToken cancelacion)
    {
        await _mediador.Send(new DesactivarMisionComando(misionId), cancelacion);
        return NoContent();
    }

    [HttpDelete("{misionId:guid}/eliminar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarMision(Guid misionId, CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarMisionComando(misionId), cancelacion);
        return NoContent();
    }

    [HttpPost("{misionId:guid}/etapas")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AgregarEtapa(
        Guid misionId,
        [FromBody] AgregarEtapaDto dto,
        CancellationToken cancelacion)
    {
        var etapaId = await _mediador.Send(new AgregarEtapaComando(misionId, dto), cancelacion);
        return Created($"/api/juegos/misiones/{misionId}/etapas/{etapaId}", new { id = etapaId });
    }

    [HttpDelete("{misionId:guid}/etapas/{etapaId:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EliminarEtapa(
        Guid misionId, Guid etapaId, CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarEtapaComando(misionId, etapaId), cancelacion);
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
