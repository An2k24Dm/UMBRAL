using MediatR;
using SesionesServicio.Commons.Dtos.Penalizaciones;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionParticipante;

public sealed record AplicarPenalizacionParticipanteComando(
    Guid SesionId,
    Guid ParticipanteSesionId,
    int Puntos,
    string? Motivo) : IRequest<PenalizacionEncoladaDto>;
