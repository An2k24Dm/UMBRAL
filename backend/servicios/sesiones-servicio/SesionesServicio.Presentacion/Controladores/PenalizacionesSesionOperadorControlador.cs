using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionEquipo;
using SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionParticipante;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones")]
[Authorize(Policy = "PoliticaSoloOperador")]
public sealed class PenalizacionesSesionOperadorControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public PenalizacionesSesionOperadorControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpPost("{sesionId:guid}/participantes/{participanteSesionId:guid}/penalizaciones")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PenalizarParticipante(
        Guid sesionId,
        Guid participanteSesionId,
        [FromBody] SolicitudPenalizacion solicitud,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new AplicarPenalizacionParticipanteComando(
                sesionId, participanteSesionId, solicitud.Puntos, solicitud.Motivo),
            cancelacion);
        return Accepted(resultado);
    }

    [HttpPost("{sesionId:guid}/equipos/{equipoId:guid}/penalizaciones")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PenalizarEquipo(
        Guid sesionId,
        Guid equipoId,
        [FromBody] SolicitudPenalizacion solicitud,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new AplicarPenalizacionEquipoComando(
                sesionId, equipoId, solicitud.Puntos, solicitud.Motivo),
            cancelacion);
        return Accepted(resultado);
    }
}

public sealed record SolicitudPenalizacion(int Puntos, string? Motivo);
