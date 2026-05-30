using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

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

    // HU33 — Verifica si existe alguna sesión vigente (Programada,
    // EnPreparacion, Activa, Pausada) asociada al contenido indicado.
    // Lo consume juegos-servicio antes de archivar/desactivar un
    // contenido para evitar que una sesión vigente quede apuntando a
    // un contenido inactivo.
    //
    // 400 si el TipoJuego no es válido o el ContenidoJuegoId es vacío;
    // 401/403 si falta token o el rol no está permitido.
    [HttpGet("contenidos/{tipoJuego}/{contenidoJuegoId:guid}/existe-vigente")]
    [ProducesResponseType(typeof(ExisteSesionVigenteRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExisteSesionVigentePorContenido(
        string tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion)
    {
        if (!Enum.TryParse<TipoJuego>(tipoJuego, ignoreCase: true, out var tipoParseado)
            || !Enum.IsDefined(typeof(TipoJuego), tipoParseado))
        {
            return BadRequest(new
            {
                codigo = "TIPO_JUEGO_INVALIDO",
                mensaje = "El tipo de juego debe ser Trivia o BusquedaTesoro."
            });
        }

        if (contenidoJuegoId == Guid.Empty)
        {
            return BadRequest(new
            {
                codigo = "CONTENIDO_JUEGO_ID_INVALIDO",
                mensaje = "El identificador del contenido del juego es obligatorio."
            });
        }

        var resultado = await _mediador.Send(
            new ExisteSesionVigentePorContenidoConsulta(tipoParseado, contenidoJuegoId),
            cancelacion);

        return Ok(resultado);
    }
}
