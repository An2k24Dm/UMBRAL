using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Api.Controladores;

[ApiController]
[Route("api/sesiones")]
[Authorize(Policy = "PoliticaAdministradorUOperador")]
public sealed class SesionesControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public SesionesControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // HU33 — Crear una sesión en vivo en estado Programada.
    [HttpPost]
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

    // HU33 — Listado de sesiones para el panel del Operador/Administrador.
    [HttpGet]
    [ProducesResponseType(typeof(List<SesionListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarSesiones(CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ListarSesionesConsulta(), cancelacion);
        return Ok(resultado);
    }

    // HU33 — Detalle de una sesión por identificador.
    [HttpGet("{id:guid}")]
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
