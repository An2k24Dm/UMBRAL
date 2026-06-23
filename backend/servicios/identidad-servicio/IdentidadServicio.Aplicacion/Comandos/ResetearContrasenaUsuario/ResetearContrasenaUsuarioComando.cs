using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ResetearContrasenaUsuario;

public sealed record ResetearContrasenaUsuarioComando(Guid IdUsuario)
    : IRequest<ResetearContrasenaRespuestaDto>;
