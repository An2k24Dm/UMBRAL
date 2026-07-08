using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.PausarSesion;

// Operación de ciclo de vida: el Operador pausa su sesión activa.
public sealed record PausarSesionComando(Guid SesionId)
    : IRequest<OperacionSesionRespuestaDto>;
