using RankingServicio.Dominio.Abstract;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Estrategias;

public sealed class EstrategiaPuntajeTriviaPorTiempo
    : IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeTrivia>
{
    public Puntaje Calcular(ContextoCalculoPuntajeTrivia contexto)
    {
        if (!contexto.EsCorrecta) return Puntaje.Cero;
        if (contexto.TiempoLimiteMs <= 0 || contexto.TiempoTardadoMs >= contexto.TiempoLimiteMs)
            return Puntaje.Cero;

        var tamanoTramo = contexto.TiempoLimiteMs / 5;
        var tramo = tamanoTramo > 0 ? contexto.TiempoTardadoMs / tamanoTramo : 0;
        var factor = 1m - tramo * 0.2m;
        var puntos = Math.Max(0, (int)(contexto.PuntajeBase * factor));
        return Puntaje.Desde(puntos);
    }
}
