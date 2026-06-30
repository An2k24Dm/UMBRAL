using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionIndividual;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Authorize(Policy = "PoliticaSoloParticipante")]
public sealed class IngresoSesionesParticipanteControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public IngresoSesionesParticipanteControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpPost("api/sesiones/participante/ingresar")]
    [ProducesResponseType(typeof(IngresarSesionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IngresarPorCodigo(
        [FromBody] IngresarSesionDto dto,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new IngresarSesionPorCodigoComando(dto), cancelacion);
        return Ok(resultado);
    }

    [HttpPost("api/sesiones/{sesionId:guid}/participante/ingresar-individual")]
    [ProducesResponseType(typeof(IngresarSesionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IngresarIndividual(
        Guid sesionId,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new IngresarSesionIndividualComando(sesionId), cancelacion);
        return Ok(resultado);
    }
}
