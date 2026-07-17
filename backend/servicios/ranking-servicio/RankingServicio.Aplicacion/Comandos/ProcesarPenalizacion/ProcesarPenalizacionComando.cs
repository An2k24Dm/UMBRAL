using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPenalizacion;

public sealed record ProcesarPenalizacionComando(
    Guid EventoId,
    Guid SesionId,
    string TipoObjetivo,
    Guid? ParticipanteSesionId,
    Guid? ParticipanteIdentidadId,
    Guid? EquipoId,
    int Puntos,
    string Motivo,
    Guid OperadorIdentidadId,
    DateTime AplicadaEnUtc) : IRequest;
