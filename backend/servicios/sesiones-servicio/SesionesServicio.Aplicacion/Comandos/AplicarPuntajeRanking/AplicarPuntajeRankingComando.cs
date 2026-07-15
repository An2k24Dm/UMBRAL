using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPuntajeRanking;

public sealed record AplicarPuntajeRankingComando(
    Guid EventoIdOrigen,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    long PuntajeGanado,
    long PuntajeTotalParticipante,
    long? PuntajeTotalEquipo,
    DateTime CalculadoEnUtc)
    : IRequest;
