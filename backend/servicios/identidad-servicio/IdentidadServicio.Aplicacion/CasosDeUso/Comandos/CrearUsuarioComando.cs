using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record CrearUsuarioComando(CrearUsuarioDto Datos)
    : IRequest<CrearUsuarioRespuestaDto>;
