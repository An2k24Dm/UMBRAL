using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SesionesServicio.Aplicacion.Comandos.AbandonarSesion;
using SesionesServicio.Aplicacion.Comandos.CancelarSesion;
using SesionesServicio.Aplicacion.Comandos.CrearSesion;
using SesionesServicio.Aplicacion.Comandos.EliminarSesion;
using SesionesServicio.Aplicacion.Comandos.IniciarSesion;
using SesionesServicio.Aplicacion.Comandos.ModificarSesion;
using SesionesServicio.Aplicacion.Comandos.PausarSesion;
using SesionesServicio.Aplicacion.Comandos.ReanudarSesion;
using SesionesServicio.Aplicacion.Consultas.ListarSesiones;
using SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;
using SesionesServicio.Aplicacion.Consultas.ObtenerProgresoTrivia;
using SesionesServicio.Aplicacion.Consultas.ObtenerSesionPorId;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Presentacion.Controladores;

[ApiController]
[Route("api/sesiones")]
[Authorize]
public sealed class SesionesControlador : ControllerBase
{
    private readonly IMediator _mediador;
    private readonly IRepositorioSesiones _repositorio;

    public SesionesControlador(IMediator mediador, IRepositorioSesiones repositorio)
    {
        _mediador = mediador;
        _repositorio = repositorio;
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

    // HU48 — Abandonar la sesión (individual) o el equipo (grupal). Solo el
    // propio Participante; el backend decide según el tipo de sesión. No se
    // recibe equipoId: el participante solo puede estar en un equipo.
    [HttpDelete("{sesionId:guid}/abandonar")]
    [Authorize(Policy = "PoliticaSoloParticipante")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AbandonarSesion(
        Guid sesionId,
        CancellationToken cancelacion)
    {
        await _mediador.Send(new AbandonarSesionComando(sesionId), cancelacion);
        return NoContent();
    }

    // Modificar sesión. Solo Operador. El Operador solo puede modificar sus
    // propias sesiones y únicamente si están en estado Programada. El
    // Administrador no realiza acciones de escritura sobre sesiones.
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(typeof(SesionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ModificarSesion(
        Guid id, [FromBody] ModificarSesionDto dto, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ModificarSesionComando(id, dto), cancelacion);
        return Ok(resultado);
    }

    // Eliminar sesión. Solo Operador, solo sus propias sesiones y únicamente
    // si están en estado Programada (HU39). El Administrador no puede eliminar.
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarSesion(Guid id, CancellationToken cancelacion)
    {
        await _mediador.Send(new EliminarSesionComando(id), cancelacion);
        return NoContent();
    }

    [HttpPatch("{id:guid}/iniciar")]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(typeof(OperacionSesionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IniciarSesion(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new IniciarSesionComando(id), cancelacion);
        return Ok(resultado);
    }

    [HttpPatch("{id:guid}/pausar")]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(typeof(OperacionSesionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PausarSesion(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new PausarSesionComando(id), cancelacion);
        return Ok(resultado);
    }

    [HttpPatch("{id:guid}/reanudar")]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(typeof(OperacionSesionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReanudarSesion(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new ReanudarSesionComando(id), cancelacion);
        return Ok(resultado);
    }

    [HttpPatch("{id:guid}/cancelar")]
    [Authorize(Policy = "PoliticaSoloOperador")]
    [ProducesResponseType(typeof(OperacionSesionRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelarSesion(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new CancelarSesionComando(id), cancelacion);
        return Ok(resultado);
    }

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

    // Progreso de trivia por participante en una sesión (para el panel del operador).
    [HttpGet("{sesionId:guid}/progreso-trivia")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerProgresoTrivia(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerProgresoTriviaConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    // Progreso completo (trivia + búsqueda del tesoro) por participante.
    [HttpGet("{sesionId:guid}/progreso")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(List<ProgresoSesionParticipanteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObtenerProgresoSesion(
        Guid sesionId, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(
            new ObtenerProgresoSesionConsulta(sesionId), cancelacion);
        return Ok(resultado);
    }

    // Endpoint interno usado por juegos-servicio para verificar si una
    // misión tiene sesiones vigentes antes de desactivarla o eliminarla.
    [HttpGet("misiones/{misionId:guid}/existe-vigente")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExisteSesionVigentePorMision(
        Guid misionId, CancellationToken cancelacion)
    {
        var existe = await _repositorio.ExisteSesionVigentePorMisionAsync(misionId, cancelacion);
        return Ok(new { existe });
    }
}
