using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Commons.Dtos;
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

    // Endpoint único de creación. El TipoUsuario del DTO selecciona la estrategia
    // (Administrador / Operador / Participante).
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
        if (dto.TipoUsuario != TipoUsuario.Participante)
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
}
