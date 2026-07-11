namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioRespuestasTrivia
{
    Task AgregarAsync(RespuestaTriviaRegistro registro, CancellationToken cancelacion);

    Task<bool> ExisteRespuestaOficialAsync(
        Guid sesionId, Guid etapaId, Guid preguntaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion);

    Task<int> ContarPreguntasDistintasDeJugadorEnEtapaAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion);

    Task<int> ContarJugadoresQueCompletaronEtapaAsync(
        Guid sesionId, Guid etapaId, int totalPreguntas, CancellationToken cancelacion);

    Task<IReadOnlyList<Guid>> ObtenerPreguntasRespondidasAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion);

    Task<IReadOnlyList<RespuestaTriviaTiempo>> ObtenerRespuestasConTiempoAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion);

    Task<IReadOnlyList<ProgresoTriviaItem>> ObtenerProgresoTriviaAsync(
        Guid sesionId, CancellationToken cancelacion);
}

public sealed record RespuestaTriviaTiempo(Guid PreguntaId, int TiempoTardadoMs);

public sealed record ProgresoTriviaItem(
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    int TotalRespondidas,
    int Correctas,
    int PuntosGanados)
{
    public ProgresoTriviaItem(
        Guid ParticipanteIdentidadId,
        int TotalRespondidas,
        int Correctas,
        int PuntosGanados)
        : this(ParticipanteIdentidadId, null, TotalRespondidas, Correctas, PuntosGanados)
    {
    }
}

public sealed record RespuestaTriviaRegistro(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid TriviaId,
    Guid PreguntaId,
    Guid? OpcionSeleccionadaId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    bool EsCorrecta,
    int PuntosGanados,
    int TiempoTardadoMs,
    DateTime FechaRespuestaUtc);
