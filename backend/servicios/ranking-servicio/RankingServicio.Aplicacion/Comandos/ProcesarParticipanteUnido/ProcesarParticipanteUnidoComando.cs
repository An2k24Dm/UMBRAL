using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;

public sealed record ProcesarParticipanteUnidoComando(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteIdentidadId,
    string NombreParticipante)
    : IRequest;
