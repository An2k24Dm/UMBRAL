using RankingServicio.Dominio.Abstract;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Estrategias;

public sealed class EstrategiaPuntajeBusquedaTesoro
    : IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeBusquedaTesoro>
{
    public Puntaje Calcular(ContextoCalculoPuntajeBusquedaTesoro contexto)
        => contexto.EsValida ? Puntaje.Desde(contexto.PuntajeBase) : Puntaje.Cero;
}
