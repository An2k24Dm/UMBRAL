using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Infraestructura.Persistencia;
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
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();

        return servicios;
    }
}
