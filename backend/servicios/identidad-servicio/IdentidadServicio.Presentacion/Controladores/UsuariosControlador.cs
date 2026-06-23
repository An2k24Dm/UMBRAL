using IdentidadServicio.Aplicacion.Comandos.ActivarOperador;
using IdentidadServicio.Aplicacion.Comandos.ActivarParticipante;
using IdentidadServicio.Aplicacion.Comandos.CrearUsuario;
using IdentidadServicio.Aplicacion.Comandos.DesactivarOperador;
using IdentidadServicio.Aplicacion.Comandos.DesactivarParticipante;
using IdentidadServicio.Aplicacion.Comandos.EliminarCuentaParticipante;
using IdentidadServicio.Aplicacion.Comandos.EliminarOperador;
using IdentidadServicio.Aplicacion.Comandos.ModificarOperador;
using IdentidadServicio.Aplicacion.Comandos.ModificarParticipante;
using IdentidadServicio.Aplicacion.Comandos.RegistrarParticipante;
using IdentidadServicio.Aplicacion.Comandos.ResetearContrasenaUsuario;
using IdentidadServicio.Aplicacion.Consultas.ConsultarParticipantes;
using IdentidadServicio.Aplicacion.Consultas.ConsultarUsuariosInternos;
using IdentidadServicio.Aplicacion.Consultas.FiltrarAdministradoresPorIds;
using IdentidadServicio.Aplicacion.Consultas.ObtenerParticipanteDetalle;
using IdentidadServicio.Aplicacion.Consultas.ObtenerUsuarioInternoDetalle;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentidadServicio.Presentacion.Controladores;

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

    [HttpPost("internos/administradores-por-ids")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(AdministradoresPorIdsRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> FiltrarAdministradoresPorIds(
        [FromBody] AdministradoresPorIdsSolicitudDto dto, CancellationToken cancelacion)
    {
        var ids = dto?.UsuariosIds ?? Array.Empty<Guid>();
        var resultado = await _mediador.Send(
            new FiltrarAdministradoresPorIdsConsulta(ids), cancelacion);
        return Ok(resultado);
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

    [HttpPatch("operadores/{id:guid}/activar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(CambiarEstadoUsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivarOperador(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new ActivarOperadorComando(id), cancelacion);
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

    [HttpPatch("participantes/{id:guid}/activar")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(CambiarEstadoUsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivarParticipante(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new ActivarParticipanteComando(id), cancelacion);
            return Ok(resultado);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("No existe un Participante", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new
            {
                codigo = "PARTICIPANTE_NO_ENCONTRADO",
                mensaje = "El participante solicitado no existe."
            });
        }
    }

    [HttpPatch("operadores/{id:guid}/desactivar")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(CambiarEstadoUsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesactivarOperador(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new DesactivarOperadorComando(id), cancelacion);
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

    [HttpPatch("participantes/{id:guid}/desactivar")]
    [Authorize(Policy = "PoliticaAdministradorUOperador")]
    [ProducesResponseType(typeof(CambiarEstadoUsuarioRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesactivarParticipante(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new DesactivarParticipanteComando(id), cancelacion);
            return Ok(resultado);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("No existe un Participante", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new
            {
                codigo = "PARTICIPANTE_NO_ENCONTRADO",
                mensaje = "El participante solicitado no existe."
            });
        }
    }

    [HttpDelete("operadores/{id:guid}")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(EliminarOperadorRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarOperador(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new EliminarOperadorComando(id), cancelacion);
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

    [HttpPost("internos/{id:guid}/resetear-contrasena")]
    [Authorize(Policy = "PoliticaAdministrador")]
    [ProducesResponseType(typeof(ResetearContrasenaRespuestaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetearContrasenaUsuarioInterno(
        Guid id, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _mediador.Send(
                new ResetearContrasenaUsuarioComando(id), cancelacion);
            return Ok(resultado);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("No existe un usuario interno", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new
            {
                codigo = "USUARIO_NO_ENCONTRADO",
                mensaje = "El usuario solicitado no existe."
            });
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
            when (ex.Message.Contains("solo aplica a Operador o Administrador", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                codigo = "ROL_NO_PERMITIDO",
                mensaje = "El reseteo administrativo de contraseña solo aplica a Operador o Administrador."
            });
        }
    }
}
