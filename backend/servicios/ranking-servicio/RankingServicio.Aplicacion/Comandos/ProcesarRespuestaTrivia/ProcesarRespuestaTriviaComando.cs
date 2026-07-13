using MediatR;

namespace RankingServicio.Aplicacion.Comandos.ProcesarRespuestaTrivia;

public sealed record ProcesarRespuestaTriviaComando(
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
    int TiempoLimiteMs)
    : IRequest;
