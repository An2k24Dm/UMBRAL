using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.AplicarResultadoPenalizacionRanking;

public sealed record AplicarResultadoPenalizacionRankingComando(
    Guid EventoIdOrigen,
    Guid SesionId,
    string TipoObjetivo,
    Guid? ParticipanteSesionId,
    Guid? ParticipanteIdentidadId,
    Guid? EquipoId,
    int PuntosPenalizados,
    int PuntosPenalizadosAcumulados,
    long? PuntajeTotalParticipante,
    long? PuntajeTotalEquipo,
    DateTime CalculadoEnUtc) : IRequest;
