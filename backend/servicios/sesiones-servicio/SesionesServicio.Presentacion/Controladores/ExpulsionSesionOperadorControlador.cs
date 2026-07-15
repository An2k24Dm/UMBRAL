using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.ExpulsarEquipoSesionGrupal;
using SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteSesionIndividual;

namespace SesionesServicio.Presentacion.Controladores;

// HU44 — Expulsar participante (sesión individual) o equipo (sesión grupal).
// Acción exclusiva del Operador creador de la sesión. La expulsión se ejecuta
// por HTTP (nunca por WebSocket); SignalR solo notifica el cambio.
[ApiController]
[Route("api/sesiones")]
[Authorize(Policy = "PoliticaSoloOperador")]
public sealed class ExpulsionSesionOperadorControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public ExpulsionSesionOperadorControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // Expulsar un participante de una sesión individual.
    [HttpDelete("{sesionId:guid}/participantes/{participanteSesionId:guid}/expulsar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExpulsarParticipante(
        Guid sesionId,
        Guid participanteSesionId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(
            new ExpulsarParticipanteSesionIndividualComando(sesionId, participanteSesionId),
            cancelacion);
        return NoContent();
    }

    // Expulsar un equipo completo de una sesión grupal.
    [HttpDelete("{sesionId:guid}/equipos/{equipoId:guid}/expulsar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExpulsarEquipo(
        Guid sesionId,
        Guid equipoId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(
            new ExpulsarEquipoSesionGrupalComando(sesionId, equipoId), cancelacion);
        return NoContent();
    }
}
