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

    Task<IReadOnlyList<ProgresoTriviaEtapaItem>> ObtenerProgresoTriviaPorEtapaAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task<int> ActualizarPuntosGanadosPorEventoAsync(
        Guid eventoPuntuacionId, int puntosGanados, CancellationToken cancelacion);

    Task<int?> ObtenerPuntajeGanadoPorEventoAsync(
        Guid eventoPuntuacionId, CancellationToken cancelacion);

    Task<IReadOnlyList<PuntajeEtapaItem>> ObtenerPuntajePorEtapaParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<long> ObtenerPuntajeGanadoEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion);
}

// Puntaje acumulado por (misión, etapa) para el desglose del historial.
public sealed record PuntajeEtapaItem(Guid MisionId, Guid EtapaId, int Puntaje);

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

public sealed record ProgresoTriviaEtapaItem(
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    Guid EtapaId,
    int PreguntasRespondidas);

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
    Guid EventoPuntuacionId,
    int TiempoTardadoMs,
    DateTime FechaRespuestaUtc);
