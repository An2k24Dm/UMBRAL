namespace RankingServicio.Commons.Dtos.Eventos.Entrada;

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
