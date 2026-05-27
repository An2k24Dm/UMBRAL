using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentidadServicio.Api.Controladores;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public UsuariosControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpPost]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(CrearUsuarioRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CrearUsuario(
        [FromBody] CrearUsuarioDto dto, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new CrearUsuarioComando(dto), cancelacion);
        return Created($"/api/usuarios/{resultado.Id}", resultado);
    }

    [HttpPost("participantes/registro")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CrearUsuarioRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarParticipante(
        [FromBody] RegistrarParticipanteDto dto, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new RegistrarParticipanteComando(dto), cancelacion);
        return Created($"/api/usuarios/{resultado.Id}", resultado);
    }

    [HttpGet("participantes")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(ResultadoPaginadoDto<ParticipanteListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConsultarParticipantes(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanioPagina = 10,
        [FromQuery] string? ordenEstado = null,
        CancellationToken cancelacion = default)
    {
        var resultado = await _mediador.Send(
            new ConsultarParticipantesConsulta(pagina, tamanioPagina, ordenEstado),
            cancelacion);
        return Ok(resultado);
    }

    [HttpGet("participantes/{id:guid}")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(PerfilParticipanteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalleParticipante(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var detalle = await _mediador.Send(
                new ObtenerParticipanteDetalleConsulta(id), cancelacion);
            return Ok(detalle);
        }
        catch (DatosUsuarioInvalidosExcepcion)
        {
            return NotFound(new
            {
                codigo = "PARTICIPANTE_NO_ENCONTRADO",
                mensaje = "El participante solicitado no existe."
            });
        }
    }

    [HttpGet("internos")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(ResultadoPaginadoDto<UsuarioInternoListadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPaginadoDto<UsuarioInternoListadoDto>>> ListarUsuariosInternos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanioPagina = 10,
        [FromQuery] string? rol = null,
        [FromQuery] string? ordenEstado = null,
        CancellationToken cancelacion = default)
    {
        var resultado = await _mediador.Send(
            new ConsultarUsuariosInternosConsulta(pagina, tamanioPagina, rol, ordenEstado),
            cancelacion);
        return Ok(resultado);
    }

    [HttpGet("internos/{id:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ObtenerUsuarioInterno(
        Guid id, CancellationToken cancelacion)
    {
        var perfil = await _mediador.Send(
            new ObtenerUsuarioInternoDetalleConsulta(id), cancelacion);

        if (perfil is null)
        {
            return NotFound(new
            {
                codigo = "USUARIO_NO_ENCONTRADO",
                mensaje = "El usuario interno solicitado no existe."
            });
        }

        return Ok((object)perfil);
    }

    [HttpPatch("participantes/perfil")]
    [Authorize(Policy = "PoliticaParticipante")]
    [ProducesResponseType(typeof(ModificarParticipanteRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModificarParticipante(
        [FromBody] ModificarParticipanteSolicitudDto dto,
        CancellationToken cancelacion)
    {

        var idKeycloak = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(idKeycloak))
        {
            return Unauthorized(new
            {
                codigo = "TOKEN_SIN_SUJETO",
                mensaje = "El token no contiene el identificador del usuario."
            });
        }

        try
        {
            var resultado = await _mediador.Send(
                new ModificarParticipanteComando(idKeycloak, dto), cancelacion);
            return Ok(resultado);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("No existe un Participante", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new
            {
                codigo = "PARTICIPANTE_NO_ENCONTRADO",
                mensaje = "El participante asociado al usuario autenticado no existe."
            });
        }
    }

    [HttpDelete("participantes/perfil")]
    [Authorize(Policy = "PoliticaParticipante")]
    [ProducesResponseType(typeof(EliminarCuentaParticipanteRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarCuentaParticipante(CancellationToken cancelacion)
    {
        var idKeycloak = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(idKeycloak))
        {
            return Unauthorized(new
            {
                codigo = "TOKEN_SIN_SUJETO",
                mensaje = "El token no contiene el identificador del usuario."
            });
        }

        try
        {
            var resultado = await _mediador.Send(
                new EliminarCuentaParticipanteComando(idKeycloak), cancelacion);
            return Ok(resultado);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("No existe un Participante", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new
            {
                codigo = "PARTICIPANTE_NO_ENCONTRADO",
                mensaje = "El participante asociado al usuario autenticado no existe."
            });
        }
    }

    [HttpPatch("operadores/{id:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(ModificarOperadorRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModificarOperador(
        Guid id,
        [FromBody] ModificarOperadorSolicitudDto dto,
        CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new ModificarOperadorComando(id, dto), cancelacion);
            return Ok(resultado);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("No existe un Operador", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new
            {
                codigo = "OPERADOR_NO_ENCONTRADO",
                mensaje = "El operador solicitado no existe."
            });
        }
    }
}
