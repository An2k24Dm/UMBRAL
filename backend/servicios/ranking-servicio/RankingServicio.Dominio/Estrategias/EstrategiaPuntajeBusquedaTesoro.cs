using RankingServicio.Dominio.Abstract;
using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Estrategias;

public sealed class EstrategiaPuntajeBusquedaTesoro
    : IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeBusquedaTesoro>
{
    public Puntaje Calcular(ContextoCalculoPuntajeBusquedaTesoro contexto)
    {
        if (!contexto.EsValida) return Puntaje.Cero;

        if (contexto.TotalCompetidores < 1)
            throw new RankingInvalidoExcepcion(
                "El total de competidores debe ser al menos 1.");
        if (contexto.OrdenResolucion < 1
            || contexto.OrdenResolucion > contexto.TotalCompetidores)
            throw new RankingInvalidoExcepcion(
                "El orden de resolución debe estar entre 1 y el total de competidores.");

        if (contexto.TotalCompetidores == 1 || contexto.OrdenResolucion == 1)
            return Puntaje.Desde(Math.Max(1, contexto.PuntajeBase));

        var penalizacion = CalcularPenalizacionPorPosicion(
            contexto.PuntajeBase, contexto.TotalCompetidores);
        var puntajeCalculado =
            contexto.PuntajeBase - penalizacion * (contexto.OrdenResolucion - 1);
        var puntajeFinal = Math.Max(1, puntajeCalculado);
        return Puntaje.Desde(puntajeFinal);
    }

    private static int CalcularPenalizacionPorPosicion(int puntajeBase, int totalCompetidores)
        => Math.Max(
            1,
            (int)Math.Round(
                (decimal)puntajeBase / totalCompetidores,
                MidpointRounding.AwayFromZero));
}
