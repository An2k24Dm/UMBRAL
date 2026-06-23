using System.Security.Claims;
using IdentidadServicio.Aplicacion.Comandos.CambiarContrasenaObligatoria;
using IdentidadServicio.Aplicacion.Comandos.IniciarSesion;
using IdentidadServicio.Aplicacion.Consultas.ObtenerPerfilActual;
using IdentidadServicio.Aplicacion.Enums;
using IdentidadServicio.Commons.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentidadServicio.Presentacion.Controladores;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionControlador : ControllerBase
{
    private readonly IMediator _mediador;

    public AutenticacionControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    [HttpPost("login-web")]
    [AllowAnonymous]
    public async Task<ActionResult<ResultadoInicioSesionDto>> LoginWeb(
        [FromBody] InicioSesionDto dto, CancellationToken cancelacion)
    {
        var comando = new IniciarSesionComando(
            dto.NombreUsuario, dto.Contrasena, OrigenInicioSesion.Web);
        var resultado = await _mediador.Send(comando, cancelacion);
        return Ok(resultado);
    }

    [HttpPost("login-movil")]
    [AllowAnonymous]
    public async Task<ActionResult<ResultadoInicioSesionDto>> LoginMovil(
        [FromBody] InicioSesionDto dto, CancellationToken cancelacion)
    {
        var comando = new IniciarSesionComando(
            dto.NombreUsuario, dto.Contrasena, OrigenInicioSesion.Movil);
        var resultado = await _mediador.Send(comando, cancelacion);
        return Ok(resultado);
    }

    [HttpGet("perfil-actual")]
    [Authorize]
    public async Task<ActionResult<object>> ObtenerPerfilActual(CancellationToken cancelacion)
    {
        var idKeycloak = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub")
                         ?? string.Empty;
        var perfil = await _mediador.Send(new ObtenerPerfilActualConsulta(idKeycloak), cancelacion);
        return Ok((object)perfil);
    }

    [HttpPost("cambiar-contrasena-obligatoria")]
    [Authorize]
    public async Task<ActionResult<CambiarContrasenaObligatoriaRespuestaDto>>
        CambiarContrasenaObligatoria(
            [FromBody] CambiarContrasenaObligatoriaDto dto,
            CancellationToken cancelacion)
    {
        var idKeycloak = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub")
                         ?? string.Empty;

        if (string.IsNullOrWhiteSpace(idKeycloak))
        {
            return Unauthorized(new
            {
                codigo = "TOKEN_SIN_SUJETO",
                mensaje = "El token no contiene el identificador del usuario."
            });
        }

        var resultado = await _mediador.Send(
            new CambiarContrasenaObligatoriaComando(idKeycloak, dto), cancelacion);
        return Ok(resultado);
    }
}
