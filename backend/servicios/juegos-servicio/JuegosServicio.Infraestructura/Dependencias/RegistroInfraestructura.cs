using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Infraestructura.Persistencia;
using JuegosServicio.Infraestructura.ServiciosExternos;
using JuegosServicio.Infraestructura.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JuegosServicio.Infraestructura.Dependencias;

public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("BaseDatos")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'BaseDatos'.");

        servicios.AddDbContext<ContextoJuegos>(opciones =>
            opciones.UseNpgsql(cadenaConexion, p =>
            {
                p.MigrationsHistoryTable("__historial_migraciones", "juegos");
                p.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        servicios.AddScoped<IRepositorioJuegos, RepositorioJuegos>();
        servicios.AddScoped<IRepositorioBusquedas, RepositorioBusquedas>();
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();

        // Cliente HTTP hacia sesiones-servicio. Se enlaza por
        // HttpClientFactory para reutilizar conexiones y permitir
        // sustituirlo en pruebas mediante un HttpMessageHandler doble.
        servicios.Configure<OpcionesSesionesServicio>(
            configuracion.GetSection(OpcionesSesionesServicio.Seccion));
        servicios.AddHttpClient<IClienteSesiones, ClienteSesionesHttp>();

        return servicios;
    }
}
