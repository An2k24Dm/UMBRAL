using Microsoft.Extensions.DependencyInjection;
using RankingServicio.Dominio.Abstract;
using RankingServicio.Dominio.Estrategias;

namespace RankingServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegistroAplicacion).Assembly));
        servicios.AddSingleton<
            IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeTrivia>,
            EstrategiaPuntajeTriviaPorTiempo>();
        servicios.AddSingleton<
            IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeBusquedaTesoro>,
            EstrategiaPuntajeBusquedaTesoro>();

        return servicios;
    }
}
