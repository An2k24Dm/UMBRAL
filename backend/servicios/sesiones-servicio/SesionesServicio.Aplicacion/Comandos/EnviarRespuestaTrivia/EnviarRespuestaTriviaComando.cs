using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;

public sealed record EnviarRespuestaTriviaComando(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid TriviaId,
    Guid PreguntaId,
    Guid? OpcionSeleccionadaId,
    int TiempoTardadoMs)
    : IRequest<EnviarRespuestaTriviaRespuesta>;

public sealed record EnviarRespuestaTriviaRespuesta(
    bool EsCorrecta,
    int PuntosGanados,
    bool EtapaCompletada);
