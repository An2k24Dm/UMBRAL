namespace SesionesServicio.Dominio.Abstract;

public sealed record EvidenciaTesoroRegistro(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid BusquedaId,
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    string CodigoEnviado,
    bool EsValida,
    int PuntosGanados,
    Guid EventoPuntuacionId,
    DateTime FechaEnvioUtc);

public sealed record ProgresoTesoroItem(
    Guid ParticipanteIdentidadId,
    Guid? EquipoId,
    int TotalIntentados,
    int Validos,
    int PuntosGanados)
{
    public ProgresoTesoroItem(
        Guid ParticipanteIdentidadId,
        int TotalIntentados,
        int Validos,
        int PuntosGanados)
        : this(ParticipanteIdentidadId, null, TotalIntentados, Validos, PuntosGanados)
    {
    }
}

public interface IRepositorioEvidenciasTesoro
{
    Task AgregarAsync(EvidenciaTesoroRegistro registro, CancellationToken cancelacion);

    // Serializa la asignación de orden de resolución de una etapa entre
    // competidores concurrentes (y entre instancias del servicio) tomando un
    // bloqueo transaccional en PostgreSQL. Debe invocarse dentro de la
    // transacción, antes de insertar la evidencia válida y contar los completados,
    // de modo que dos competidores no obtengan el mismo orden. Se libera al
    // confirmar/revertir la transacción.
    Task BloquearEtapaParaOrdenAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);

    Task<bool> ExisteEvidenciaValidaIndividualAsync(
        Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<bool> ExisteEvidenciaValidaEquipoAsync(
        Guid sesionId, Guid etapaId, Guid equipoId, CancellationToken cancelacion);

    Task<bool> ExisteEvidenciaIndividualAsync(
        Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<bool> ExisteEvidenciaEquipoAsync(
        Guid sesionId, Guid etapaId, Guid equipoId, CancellationToken cancelacion);

    Task<int> ContarParticipantesConEvidenciaValidaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);

    Task<int> ContarEquiposConEvidenciaValidaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion);

    Task<IReadOnlyList<ProgresoTesoroItem>> ObtenerProgresoTesoroAsync(
        Guid sesionId, CancellationToken cancelacion);

    // Fija el puntaje real (calculado por ranking) en la evidencia cuyo
    // EventoPuntuacionId coincide. Idempotente. Devuelve las filas afectadas.
    Task<int> ActualizarPuntosGanadosPorEventoAsync(
        Guid eventoPuntuacionId, int puntosGanados, CancellationToken cancelacion);

    // Desglose del puntaje por etapa de un participante (SUM de PuntosGanados
    // agrupado por misión y etapa).
    Task<IReadOnlyList<PuntajeEtapaItem>> ObtenerPuntajePorEtapaParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion);

    // HU52 — Puntaje bruto ganado por un equipo completo (SUM de PuntosGanados
    // de todos sus integrantes). Usado por el desglose grupal.
    Task<long> ObtenerPuntajeGanadoEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion);
}
