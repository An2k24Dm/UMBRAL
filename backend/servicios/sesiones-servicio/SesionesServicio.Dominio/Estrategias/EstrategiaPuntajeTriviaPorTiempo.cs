using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Dominio.Estrategias;

// Política de puntuación por tiempo: penalización en 5 tramos de 20 %.
// Regla idéntica a la que antes vivía dentro del manejador:
//   - Respuesta incorrecta                 → 0 puntos.
//   - Tiempo >= límite (o límite <= 0)      → 0 puntos.
//   - Dentro del límite: cada tramo de 20 % del tiempo consumido resta 20 %
//     del puntaje base.
// Ejemplo (5 pts, límite 10 s): 0-2s=5, 2-4s=4, 4-6s=3, 6-8s=2, 8-10s=1, >=10s=0.
public sealed class EstrategiaPuntajeTriviaPorTiempo : IEstrategiaCalculoPuntajeTrivia
{
    public int Calcular(ContextoCalculoPuntajeTrivia contexto)
    {
        if (!contexto.EsCorrecta) return 0;
        if (contexto.TiempoLimiteMs <= 0 || contexto.TiempoTardadoMs >= contexto.TiempoLimiteMs)
            return 0;

        var tamanoTramo = contexto.TiempoLimiteMs / 5;
        var tramo = tamanoTramo > 0 ? contexto.TiempoTardadoMs / tamanoTramo : 0;
        return Math.Max(0, (int)(contexto.PuntajeBase * (1.0 - tramo * 0.2)));
    }
}
