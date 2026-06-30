using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Aplicacion.Comandos.EliminarEquipo;
using SesionesServicio.Aplicacion.Comandos.ModificarEquipo;
using SesionesServicio.Aplicacion.Consultas.ListarEquiposSesion;
using SesionesServicio.Aplicacion.Consultas.ObtenerDetalleEquipoSesion;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones")]
[Authorize]
public sealed class EquiposControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public EquiposControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // HU40 — Crear equipo en una sesión grupal En Preparación. Solo
    // Participante. El líder se resuelve del usuario autenticado, no del body.
    [HttpPost("{sesionId:guid}/equipos")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(CrearEquipoRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearEquipo(
        Guid sesionId,
        [FromBody] CrearEquipoDto dto,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new CrearEquipoComando(sesionId, dto), cancelacion);

        return Created(
            $"/api/sesiones/{sesionId}/equipos/{resultado.Id}", resultado);
    }

    // HU43/HU44 — Listar equipos de una sesión grupal. Participante, Operador
    // o Administrador (este último en modo solo lectura).
    [HttpGet("{sesionId:guid}/equipos")]
    [Authorize(Policy = "PoliticaAdministradorOperadorOParticipante")]
    [ProducesResponseType(typeof(List<EquipoSesionListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ListarEquipos(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ListarEquiposSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    // HU41 — Modificar un equipo. Solo el líder (Participante). El líder se
    // resuelve del usuario autenticado, no del body.
    [HttpPut("{sesionId:guid}/equipos/{equipoId:guid}")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(typeof(ModificarEquipoRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ModificarEquipo(
        Guid sesionId,
        Guid equipoId,
        [FromBody] ModificarEquipoDto dto,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ModificarEquipoComando(sesionId, equipoId, dto), cancelacion);
        return Ok(resultado);
    }

    // HU42 — Eliminar un equipo. Solo el líder (Participante). El líder se
    // resuelve del usuario autenticado, no del body.
    [HttpDelete("{sesionId:guid}/equipos/{equipoId:guid}")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarEquipo(
        Guid sesionId,
        Guid equipoId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarEquipoComando(sesionId, equipoId), cancelacion);
        return NoContent();
    }

    // HU43/HU44 — Detalle de un equipo de la sesión. Participante, Operador o
    // Administrador (este último en modo solo lectura).
    [HttpGet("{sesionId:guid}/equipos/{equipoId:guid}")]
    [Authorize(Policy = "PoliticaAdministradorOperadorOParticipante")]
    [ProducesResponseType(typeof(EquipoSesionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ObtenerDetalleEquipo(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerDetalleEquipoSesionConsulta(sesionId, equipoId), cancelacion);
        return Ok(resultado);
    }
}
