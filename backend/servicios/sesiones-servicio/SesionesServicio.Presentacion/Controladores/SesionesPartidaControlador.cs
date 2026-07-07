using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using EstadoPartidaDto = SesionesServicio.Commons.Dtos.EstadoPartidaDto;

namespace SesionesServicio.Presentacion.Controladores;

/// <summary>
/// Endpoints consultados por partidas-servicio para validar estado de sesión y membresía.
/// </summary>
[ApiController]
[Route("api/sesiones")]
[Authorize]
public sealed class SesionesPartidaControlador : ControllerBase
{
    private readonly IConsultasSesiones _consultas;
    private readonly IUsuarioActual _usuario;

    public SesionesPartidaControlador(IConsultasSesiones consultas, IUsuarioActual usuario)
    {
        _consultas = consultas;
        _usuario = usuario;
    }

    /// <summary>
    /// HU-37: Consultado por partidas-servicio para verificar estado de la sesión
    /// y si el participante autenticado está inscrito.
    /// </summary>
    [HttpGet("{sesionId:guid}/estado-partida")]
    [ProducesResponseType(typeof(EstadoPartidaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerEstadoPartida(
        Guid sesionId, CancellationToken cancelacion)
    {
        var participanteId = _usuario.ObtenerId();
        if (participanteId is null)
            return Unauthorized();

        var resultado = await _consultas.ObtenerEstadoPartidaAsync(
            sesionId, participanteId.Value, cancelacion);

        if (resultado is null)
            return NotFound(new { codigo = "SESION_NO_ENCONTRADA", mensaje = "Sesión no encontrada." });

        return Ok(new EstadoPartidaDto
        {
            Estado = resultado.Estado,
            ParticipanteInscrito = resultado.ParticipanteInscrito,
            EquipoId = resultado.EquipoId
        });
    }

    /// <summary>
    /// Devuelve nombres de equipos e IDs de participantes individuales para enriquecer el ranking.
    /// Llamado por partidas-servicio.
    /// </summary>
    [HttpGet("{sesionId:guid}/nombres-ranking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerNombresRanking(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _consultas.ObtenerNombresRankingAsync(sesionId, cancelacion);
        return Ok(resultado);
    }
}
