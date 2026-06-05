using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones")]
[Authorize]
public sealed class SesionesControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public SesionesControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // Crear sesión. Sólo Operador.
    [HttpPost]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(typeof(CrearSesionRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearSesion(
        [FromBody] CrearSesionSolicitudDto dto, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new CrearSesionComando(dto), cancelacion);
        return Created($"/api/sesiones/{resultado.Id}", resultado);
    }

    // Listado de sesiones. Administrador ve todas, Operador sólo las propias.
    [HttpGet]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(List<SesionListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarSesiones(
        [FromQuery] string? estado, CancellationToken cancelacion)
    {
        EstadoSesion? estadoFiltro = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EstadoSesion>(estado, ignoreCase: true, out var es)
                || !Enum.IsDefined(typeof(EstadoSesion), es))
            {
                return BadRequest(new
                {
                    codigo = "ESTADO_SESION_INVALIDO",
                    mensaje = "El estado debe ser Programada, EnPreparacion, Activa, Pausada, Finalizada o Cancelada."
                });
            }
            estadoFiltro = es;
        }

        var resultado = await _mediador.Send(new ListarSesionesConsulta(estadoFiltro), cancelacion);
        return Ok(resultado);
    }

    // Detalle de una sesión. Administrador puede ver cualquiera; Operador,
    // sólo las que él creó.
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(SesionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerSesion(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ObtenerSesionPorIdConsulta(id), cancelacion);
        if (resultado is null)
            return NotFound(new
            {
                codigo = "SESION_NO_ENCONTRADA",
                mensaje = "La sesión solicitada no existe."
            });
        return Ok(resultado);
    }
}
