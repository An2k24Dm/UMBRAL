using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentidadServicio.Api.Controladores;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosControlador : ControllerBase
{
    private readonly IMediator _mediador;
    private readonly IAuthorizationService _autorizacion;

    public UsuariosControlador(IMediator mediador, IAuthorizationService autorizacion)
    {
        _mediador = mediador;
        _autorizacion = autorizacion;
    }

    // Endpoint único de creación. El TipoUsuario del DTO (mapeado a RolUsuario)
    // selecciona la estrategia (Administrador / Operador / Participante).
    //
    // HU02: crear Operador o Administrador exige token de Administrador.
    // Participante queda abierto (registro público desde la app móvil — HU03).
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CrearUsuarioRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CrearUsuario(
        [FromBody] CrearUsuarioDto dto, CancellationToken cancelacion)
    {
        if (dto.TipoUsuario != RolUsuario.Participante)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    codigo = "NO_AUTENTICADO",
                    mensaje = "Debe iniciar sesión como administrador."
                });
            }

            var autorizado = await _autorizacion.AuthorizeAsync(User, "PoliticaAdministrador");
            if (!autorizado.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    codigo = "ACCESO_NO_PERMITIDO",
                    mensaje = "No tiene permisos para crear este tipo de usuario."
                });
            }
        }

        var resultado = await _mediador.Send(new CrearUsuarioComando(dto), cancelacion);
        return Created($"/api/usuarios/{resultado.Id}", resultado);
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
