using System.Reflection;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        servicios.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ensamblado));

        // Patrón Strategy: una implementación por cada tipo de usuario.
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearAdministrador>();
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearOperador>();
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearParticipante>();

        // Patrón Factory: recibe el IEnumerable<IEstrategiaCreacionUsuario>.
        servicios.AddScoped<FabricaEstrategiaCreacionUsuario>();

        return servicios;
    }
}
