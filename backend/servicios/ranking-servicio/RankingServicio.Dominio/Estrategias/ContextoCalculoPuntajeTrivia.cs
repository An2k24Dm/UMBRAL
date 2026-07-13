namespace RankingServicio.Dominio.Estrategias;

public sealed record ContextoCalculoPuntajeTrivia(
    bool EsCorrecta,
    int PuntajeBase,
    int TiempoTardadoMs,
    int TiempoLimiteMs);
