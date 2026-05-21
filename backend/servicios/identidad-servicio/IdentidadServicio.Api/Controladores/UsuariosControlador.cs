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

    // HU02 — registro de Operador o Administrador desde el panel web. Exige
    // token de Administrador y el validador rechaza Participante (que sólo
    // puede registrarse por el endpoint público de HU03 /participantes/registro).
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

    // HU03 — registro público de Participante desde la app móvil. Sin token:
    // el backend asigna RolUsuario.Participante internamente y nunca permite
    // crear Operador/Administrador por esta vía.
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

    // HU07: listado paginado de Participantes. Protegido para Administrador y
    // Operador; Participante no puede acceder al panel web.
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

    // HU07: detalle de un Participante. Si el id no corresponde a un
    // Participante (no existe o es un usuario interno) se responde 404.
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

    // HU08 — listado paginado de cuentas internas (Operador / Administrador).
    // Restringido a Administrador (la política replica la seguridad del frontend).
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

    // HU08 — detalle de un usuario interno. Se devuelve como object para que
    // System.Text.Json serialice las propiedades del tipo derivado
    // (PerfilOperadorDto / PerfilAdministradorDto).
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
}
