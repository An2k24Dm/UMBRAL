namespace RankingServicio.Commons.Dtos.Eventos.Entrada;

public sealed record EventoParticipanteUnidoSesion(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId);
