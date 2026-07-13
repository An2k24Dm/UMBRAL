using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;

public sealed record ProcesarParticipanteUnidoComando(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId)
    : IRequest;
