using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteSesionIndividual;

// HU44 — SesionId y ParticipanteSesionId viajan en la ruta; el operador se
// resuelve del usuario autenticado, no del body. No devuelve cuerpo (204).
public sealed record ExpulsarParticipanteSesionIndividualComando(
    Guid SesionId,
    Guid ParticipanteSesionId) : IRequest;
