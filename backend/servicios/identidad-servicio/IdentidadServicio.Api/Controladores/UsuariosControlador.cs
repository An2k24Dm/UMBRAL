using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
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
}
