using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.ReanudarSesion;

// Operación de ciclo de vida: el Operador reanuda su sesión pausada.
public sealed record ReanudarSesionComando(Guid SesionId)
    : IRequest<OperacionSesionRespuestaDto>;
