using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartidasServicio.Aplicacion.Consultas.ObtenerRankingSesion;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Presentacion.Controladores;

[ApiController]
[Route("api/partidas")]
public sealed class RankingControlador : ControllerBase
{
    private readonly IMediator _mediator;

    public RankingControlador(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene el ranking de la sesión, ordenado por puntaje desc y tiempo asc.
    /// Accesible a cualquier usuario autenticado.
    /// </summary>
    [HttpGet("sesiones/{sesionId:guid}/ranking")]
    [Authorize(Policy = "PoliticaAutenticado")]
    [ProducesResponseType(typeof(IReadOnlyList<RankingEntradaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerRanking(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediator.Send(new ObtenerRankingSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }
}
