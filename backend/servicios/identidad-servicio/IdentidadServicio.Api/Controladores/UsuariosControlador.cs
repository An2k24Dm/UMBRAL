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

    public UsuariosControlador(IMediator mediador)
    {
        _mediador = mediador;
    }

    // Único endpoint de creación. El TipoUsuario del DTO selecciona la
    // estrategia (Administrador / Operador / Participante).
    //
    // En etapa académica permanece AllowAnonymous para permitir el alta del
    // primer administrador (bootstrap). En producción esto se cambiaría a
    // [Authorize(Policy = "PoliticaAdministrador")] para crear administradores
    // y operadores, dejando participantes abiertos para la app móvil.
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CrearUsuarioRespuestaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CrearUsuarioRespuestaDto>> CrearUsuario(
        [FromBody] CrearUsuarioDto dto, CancellationToken cancelacion)
    {
        var resultado = await _mediador.Send(new CrearUsuarioComando(dto), cancelacion);
        return Created($"/api/usuarios/{resultado.Id}", resultado);
    }
}
