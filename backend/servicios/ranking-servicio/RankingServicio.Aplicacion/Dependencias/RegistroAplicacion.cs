using Microsoft.Extensions.DependencyInjection;

namespace RankingServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegistroAplicacion).Assembly));

        return servicios;
    }
}
