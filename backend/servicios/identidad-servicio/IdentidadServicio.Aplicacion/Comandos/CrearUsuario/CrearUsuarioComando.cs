using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.CrearUsuario;

public sealed record CrearUsuarioComando(CrearUsuarioDto Datos)
    : IRequest<CrearUsuarioRespuestaDto>;
