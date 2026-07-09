namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioRespuestasTrivia
{
    Task AgregarAsync(RespuestaTriviaRegistro registro, CancellationToken cancelacion);

    Task<bool> ExisteRespuestaAsync(
        Guid sesionId, Guid etapaId, Guid preguntaId,
        Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<int> ContarRespuestasDeJugadorEnEtapaAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<int> ContarJugadoresQueCompletaronEtapaAsync(
        Guid sesionId, Guid etapaId, int totalPreguntas, CancellationToken cancelacion);

    Task<IReadOnlyList<Guid>> ObtenerPreguntasRespondidasAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<IReadOnlyList<ProgresoTriviaItem>> ObtenerProgresoTriviaAsync(
        Guid sesionId, CancellationToken cancelacion);
}

public sealed record ProgresoTriviaItem(
    Guid ParticipanteIdentidadId,
    int TotalRespondidas,
    int Correctas,
    int PuntosGanados);

public sealed record RespuestaTriviaRegistro(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid TriviaId,
    Guid PreguntaId,
    Guid OpcionSeleccionadaId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    bool EsCorrecta,
    int PuntosGanados,
    int TiempoTardadoMs,
    DateTime FechaRespuestaUtc);
