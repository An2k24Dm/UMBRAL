using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.CancelarSesion;

// Operación de ciclo de vida: el Operador cancela su sesión en vivo
// (EnPreparacion, Activa o Pausada).
public sealed record CancelarSesionComando(Guid SesionId)
    : IRequest<OperacionSesionRespuestaDto>;
