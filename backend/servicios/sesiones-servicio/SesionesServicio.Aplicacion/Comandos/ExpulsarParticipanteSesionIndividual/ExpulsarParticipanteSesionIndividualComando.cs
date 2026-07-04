using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteSesionIndividual;

public sealed record ExpulsarParticipanteSesionIndividualComando(
    Guid SesionId,
    Guid ParticipanteSesionId) : IRequest;
