using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.IniciarSesion;

// Operación de ciclo de vida: el Operador inicia su sesión. La coordina la
// fachada de operación de sesión (patrón Facade).
public sealed record IniciarSesionComando(Guid SesionId)
    : IRequest<OperacionSesionRespuestaDto>;
