namespace PartidasServicio.Aplicacion.Estrategias;

// Strategy: el puntaje máximo se obtiene respondiendo rápido.
// Factor oscila entre 1.0 (respuesta instantánea) y 0.5 (tiempo agotado).
public sealed class CalculadoraPuntajePorTiempo : ICalculadoraPuntaje
{
    public int Calcular(int puntajeBase, long tiempoTardadoMs, long tiempoLimiteMs)
    {
        if (puntajeBase <= 0) return 0;
        if (tiempoLimiteMs <= 0) return puntajeBase;

        var tiempoAcotado = Math.Min(tiempoTardadoMs, tiempoLimiteMs);
        var factor = 1.0 - 0.5 * ((double)tiempoAcotado / tiempoLimiteMs);
        return (int)Math.Round(puntajeBase * factor);
    }
}
