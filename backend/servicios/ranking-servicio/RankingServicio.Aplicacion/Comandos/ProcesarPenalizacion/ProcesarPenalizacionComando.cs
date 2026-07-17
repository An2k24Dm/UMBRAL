using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPenalizacion;

// HU52 — Comando de aplicación de penalización en Ranking. Origen: evento
// sesion.penalizacion_aplicada. TipoObjetivo "Participante" (individual) o
// "Equipo" (grupal). La cantidad recibida siempre es positiva; Ranking la
// interpreta como delta negativo.
public sealed record ProcesarPenalizacionComando(
    Guid EventoId,
    Guid PenalizacionId,
    Guid SesionId,
    string TipoObjetivo,
    Guid? ParticipanteSesionId,
    Guid? ParticipanteIdentidadId,
    Guid? EquipoId,
    int Puntos,
    string Motivo,
    Guid OperadorIdentidadId,
    DateTime AplicadaEnUtc) : IRequest;
