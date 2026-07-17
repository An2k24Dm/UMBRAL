using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.AplicarResultadoPenalizacionRanking;

// HU52 — Resultado de ranking.penalizacion_procesada. Sesiones actualiza sus
// snapshots (participante o equipo) y marca la penalización como Procesada.
public sealed record AplicarResultadoPenalizacionRankingComando(
    Guid EventoIdOrigen,
    Guid PenalizacionId,
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
