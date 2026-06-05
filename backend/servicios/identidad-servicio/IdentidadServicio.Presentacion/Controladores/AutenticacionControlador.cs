using System.Security.Claims;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Consultas;
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

    // Login desde el panel web: sólo Administrador y Operador.
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

    // Login desde la app móvil: sólo Participante.
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

    // HU06: el perfil devuelto es siempre una instancia derivada
    // (PerfilAdministradorDto, PerfilOperadorDto o PerfilParticipanteDto). Para
    // que System.Text.Json serialice las propiedades del tipo concreto (y no
    // solo las del DTO base declarado), se retorna como object.
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
}
