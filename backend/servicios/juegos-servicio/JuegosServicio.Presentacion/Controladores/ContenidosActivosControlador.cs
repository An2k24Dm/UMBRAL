using JuegosServicio.Aplicacion.Consultas.ObtenerContenidoActivo;
using JuegosServicio.Commons.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JuegosServicio.Presentacion.Controladores;

// HU33 — Endpoint consultado por sesiones-servicio para validar que el
// contenido de juego seleccionado existe y está Activo antes de crear
// una sesión en vivo. Devuelve también el nombre del contenido, que
// sesiones-servicio guarda como snapshot en el agregado Sesion.
[ApiController]
[Route("api/juegos/contenidos-activos")]
[Authorize(Policy = "PoliticaOperador")]
public sealed class ContenidosActivosControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public ContenidosActivosControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpGet("{tipoJuego}/{contenidoId:guid}")]
    [ProducesResponseType(typeof(ContenidoJuegoActivoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(
        string tipoJuego, Guid contenidoId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerContenidoActivoConsulta(tipoJuego, contenidoId), cancelacion);

        if (resultado is null)
            return NotFound(new
            {
                codigo = "CONTENIDO_NO_ENCONTRADO",
                mensaje = "El contenido solicitado no existe."
            });

        return Ok(resultado);
    }
}
