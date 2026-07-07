using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartidasServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;
using PartidasServicio.Aplicacion.Consultas.ObtenerRankingSesion;
using PartidasServicio.Commons.Dtos;
using PartidasServicio.Dominio.Abstract;

namespace PartidasServicio.Presentacion.Controladores;

[ApiController]
[Route("api/partidas")]
public sealed class RankingControlador : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepositorioPartidas _repositorio;

    public RankingControlador(IMediator mediator, IRepositorioPartidas repositorio)
    {
        _mediator = mediator;
        _repositorio = repositorio;
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

    /// <summary>
    /// Devuelve los IDs de las preguntas ya respondidas por el participante/equipo en una etapa.
    /// Usado por la app móvil para reanudar desde la primera pregunta no respondida.
    /// </summary>
    [HttpGet("sesiones/{sesionId:guid}/misiones/{misionId:guid}/etapas/{etapaId:guid}/preguntas-respondidas")]
    [Authorize(Policy = "PoliticaAutenticado")]
    [ProducesResponseType(typeof(IReadOnlyList<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerPreguntasRespondidas(
        Guid sesionId, Guid misionId, Guid etapaId, CancellationToken cancelacion)
    {
        var resultado = await _mediator.Send(
            new ObtenerPreguntasRespondidasConsulta(sesionId, misionId, etapaId), cancelacion);
        return Ok(resultado);
    }

    /// <summary>
    /// Devuelve el estado actual de la partida para una sesión.
    /// El participante móvil lo consulta para saber si puede jugar.
    /// </summary>
    [HttpGet("sesiones/{sesionId:guid}/estado")]
    [Authorize(Policy = "PoliticaAutenticado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerEstadoPartida(
        Guid sesionId, CancellationToken cancelacion)
    {
        var partida = await _repositorio.ObtenerPorSesionIdAsync(sesionId, cancelacion);
        if (partida is null)
            return Ok(new { existe = false, estado = (string?)null, estaActiva = false });

        return Ok(new
        {
            existe = true,
            estado = partida.NombreEstado,
            estaActiva = partida.EstaActiva
        });
    }
}
