namespace SesionesServicio.Dominio.Abstract;

public sealed record EvidenciaTesoroRegistro(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid BusquedaId,
    Guid ParticipanteIdentidadId,
    string CodigoEnviado,
    bool EsValida,
    int PuntosGanados,
    DateTime FechaEnvioUtc);

public sealed record ProgresoTesoroItem(
    Guid ParticipanteIdentidadId,
    int TotalIntentados,
    int Validos,
    int PuntosGanados);

public interface IRepositorioEvidenciasTesoro
{
    Task AgregarAsync(EvidenciaTesoroRegistro registro, CancellationToken cancelacion);
    Task<bool> ExisteEvidenciaValidaAsync(Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion);
    Task<bool> ExisteEvidenciaAsync(Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion);
    Task<int> ContarParticipantesConEvidenciaValidaAsync(Guid sesionId, Guid etapaId, CancellationToken cancelacion);
    Task<IReadOnlyList<ProgresoTesoroItem>> ObtenerProgresoTesoroAsync(Guid sesionId, CancellationToken cancelacion);
}
