namespace RankingServicio.Infraestructura.RabbitMq;

// Contratos de los eventos que ranking-servicio consume desde sesiones-servicio.
// Solo transportan lo estrictamente necesario para construir el dominio de
// ranking por identificadores; los nombres/alias se enriquecen al consultar.

public sealed record EventoRespuestaTriviaRegistrada(
    Guid EventoId,
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    Guid TriviaId,
    Guid PreguntaId,
    bool EsCorrecta,
    int PuntajeBase,
    int TiempoTardadoMs,
    int TiempoLimiteMs);

public sealed record EventoEvidenciaTesoroRegistrada(
    Guid EventoId,
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid ParticipanteSesionId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    Guid BusquedaId,
    bool EsValida,
    int PuntajeBase);

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
