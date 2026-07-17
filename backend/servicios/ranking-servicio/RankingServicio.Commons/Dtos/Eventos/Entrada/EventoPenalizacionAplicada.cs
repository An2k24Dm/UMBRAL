namespace RankingServicio.Commons.Dtos.Eventos.Entrada;

public sealed record EventoPenalizacionAplicada(
    Guid EventoId,
    Guid SesionId,
    string TipoObjetivo,
    Guid? ParticipanteSesionId,
    Guid? ParticipanteIdentidadId,
    Guid? EquipoId,
    int Puntos,
    string Motivo,
    Guid OperadorIdentidadId,
    DateTime AplicadaEnUtc);
