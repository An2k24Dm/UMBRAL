using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
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
}
