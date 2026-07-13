namespace RankingServicio.Infraestructura.RabbitMq;

// Contratos de los eventos que ranking-servicio consume desde sesiones-servicio.
// Solo transportan lo estrictamente necesario para construir el dominio de
// ranking por identificadores; los nombres/alias se enriquecen al consultar.

public sealed record EventoRespuestaTriviaRegistrada(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    int Puntaje);

public sealed record EventoEvidenciaTesoroRegistrada(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    int Puntaje);

public sealed record EventoParticipanteUnidoSesion(
    Guid EventoId,
    Guid SesionId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId);

public sealed record EventoEquipoCreadoSesion(
    Guid EventoId,
    Guid SesionId,
    Guid EquipoId);
