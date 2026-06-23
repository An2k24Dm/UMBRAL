using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Consultas.ListarSesionesDisponiblesParticipante;
using SesionesServicio.Aplicacion.Consultas.ObtenerDetalleSesionDisponibleParticipante;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones/participante/disponibles")]
[Authorize(Policy = "PoliticaSoloParticipante")]
public sealed class SesionesParticipanteControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public SesionesParticipanteControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // GET /api/sesiones/participante/disponibles?busqueda=...&modo=Individual
    [HttpGet]
    [ProducesResponseType(typeof(List<SesionDisponibleMovilDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarDisponibles(
        [FromQuery] string? busqueda,
        [FromQuery] string? modo,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ListarSesionesDisponiblesParticipanteConsulta(busqueda, modo),
            cancelacion);
        return Ok(resultado);
    }

    // GET /api/sesiones/participante/disponibles/{sesionId}
    [HttpGet("{sesionId:guid}")]
    [ProducesResponseType(typeof(SesionDetalleMovilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(
        Guid sesionId,
        CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerDetalleSesionDisponibleParticipanteConsulta(sesionId),
            cancelacion);
        return Ok(resultado);
    }
}
