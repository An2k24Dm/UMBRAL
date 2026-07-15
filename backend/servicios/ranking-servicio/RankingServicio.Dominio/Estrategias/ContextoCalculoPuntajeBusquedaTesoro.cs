namespace RankingServicio.Dominio.Estrategias;

public sealed record ContextoCalculoPuntajeBusquedaTesoro(
    bool EsValida,
    int PuntajeBase,
    int OrdenResolucion,
    int TotalCompetidores,
    int TiempoTranscurridoMs,
    int TiempoLimiteMs);
