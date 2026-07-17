namespace RankingServicio.Commons.Dtos.Eventos.Entrada;

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
