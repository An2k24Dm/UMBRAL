using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ResetearContrasenaUsuarioComando(Guid IdUsuario)
    : IRequest<ResetearContrasenaRespuestaDto>;
