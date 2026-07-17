using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;
using RankingServicio.Commons.Dtos.Consultas;

namespace RankingServicio.Presentacion.Controladores;

[ApiController]
[Route("api/ranking")]
[Authorize]
public sealed class RankingController : ControllerBase
{
    private readonly IMediator _mediator;

    public RankingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("sesiones/{sesionId:guid}/participantes")]
    public async Task<IActionResult> ObtenerRankingParticipantes(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediator.Send(
            new ObtenerRankingParticipantesSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    [HttpGet("sesiones/{sesionId:guid}/equipos")]
    public async Task<IActionResult> ObtenerRankingEquipos(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediator.Send(
            new ObtenerRankingEquiposSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    [HttpGet("global")]
    [Authorize(Policy = "PoliticaParticipante")]
    public async Task<IActionResult> ObtenerRankingGlobal(
        [FromQuery] int top,
        CancellationToken cancelacion)
    {
        var resultado = await _mediator.Send(
            new ObtenerRankingGlobalConsulta(top), cancelacion);
        return Ok(resultado);
    }
}
