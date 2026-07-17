using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Consultas.ListarSesionesDisponiblesParticipante;
using SesionesServicio.Aplicacion.Consultas.ObtenerDetalleSesionDisponibleParticipante;
using SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;
using SesionesServicio.Aplicacion.Consultas.ObtenerMisParticipaciones;
using SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSecuencialSesion;
using SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;
using SesionesServicio.Aplicacion.Consultas.ObtenerResultadoPuntaje;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Commons.Dtos.DesgloseSesion;
using SesionesServicio.Commons.Dtos.ResultadosPuntaje;

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

    // GET /api/sesiones/participante/disponibles/{sesionId}/mi-desglose
    // Desglose del puntaje del participante autenticado por misión y etapa.
    [HttpGet("{sesionId:guid}/mi-desglose")]
    [ProducesResponseType(typeof(MiDesgloseSesionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerMiDesglose(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerMiDesgloseSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    // GET /api/sesiones/participante/disponibles/{sesionId}/progreso
    [HttpGet("{sesionId:guid}/progreso")]
    [ProducesResponseType(typeof(List<ProgresoSesionParticipanteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerProgresoSesion(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerProgresoSesionConsulta(sesionId), cancelacion);
        return Ok(resultado.Filas);
    }

    // GET /api/sesiones/participante/disponibles/{sesionId}/progreso-secuencial
    [HttpGet("{sesionId:guid}/progreso-secuencial")]
    [ProducesResponseType(typeof(ProgresoSecuencialSesionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerProgresoSecuencialSesion(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerProgresoSecuencialSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    // GET /api/sesiones/participante/disponibles/resultados-puntaje/{eventoId}
    [HttpGet("resultados-puntaje/{eventoId:guid}")]
    [ProducesResponseType(typeof(ResultadoPuntajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerResultadoPuntaje(
        Guid eventoId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerResultadoPuntajeConsulta(eventoId), cancelacion);
        return Ok(resultado);
    }

    // GET /api/sesiones/participante/disponibles/finalizadas?limite=20
    [HttpGet("finalizadas")]
    [ProducesResponseType(typeof(List<MiParticipacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerMisParticipaciones(
        [FromQuery] int limite = 20,
        CancellationToken cancelacion = default)
    {
        var resultado = await _mediador.Send(
            new ObtenerMisParticipacionesConsulta(limite),
            cancelacion);
        return Ok(resultado);
    }
}
