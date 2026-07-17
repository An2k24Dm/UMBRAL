namespace RankingServicio.Infraestructura.RabbitMq;

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
    int PuntajeBase,
    int OrdenResolucion,
    int TotalCompetidores,
    int TiempoTranscurridoMs,
    int TiempoLimiteMs);

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

public sealed record EventoPenalizacionAplicada(
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
    DateTime AplicadaEnUtc);
