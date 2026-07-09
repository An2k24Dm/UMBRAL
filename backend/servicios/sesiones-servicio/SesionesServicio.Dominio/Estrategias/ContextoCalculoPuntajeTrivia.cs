namespace SesionesServicio.Dominio.Estrategias;

// Datos mínimos y suficientes para calcular el puntaje de una respuesta de
// trivia. No incluye identificadores ni dependencias de infraestructura: es
// un objeto de entrada puro para la estrategia de puntuación.
public sealed record ContextoCalculoPuntajeTrivia(
    bool EsCorrecta,
    int PuntajeBase,
    int TiempoTardadoMs,
    int TiempoLimiteMs);
