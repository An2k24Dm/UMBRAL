using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Infraestructura.Persistencia;
using IdentidadServicio.Infraestructura.Persistencia.Repositorios;
using IdentidadServicio.Infraestructura.Seguridad;
using IdentidadServicio.Infraestructura.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.Infraestructura.Dependencias;

public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("BaseDatos")
                             ?? throw new InvalidOperationException(
                                 "Falta la cadena de conexión 'BaseDatos'.");

        servicios.AddDbContext<ContextoIdentidad>(opciones =>
            opciones.UseNpgsql(cadenaConexion, p =>
            {
                p.MigrationsHistoryTable("__historial_migraciones", "identidad");
                p.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        // Repositorios segregados por responsabilidad (ISP). Cada uno comparte
        // ContextoIdentidad (Scoped), de modo que los Add/Update se acumulan
        // en el mismo DbContext y la UnidadTrabajo los confirma de un golpe.
        servicios.AddScoped<IRepositorioUsuariosLectura, RepositorioUsuariosLectura>();
        servicios.AddScoped<IRepositorioOperadores, RepositorioOperadores>();
        servicios.AddScoped<IRepositorioParticipantes, RepositorioParticipantes>();
        servicios.AddScoped<IRepositorioAdministradores, RepositorioAdministradores>();
        servicios.AddScoped<IRepositorioUnicidadUsuario, RepositorioUnicidadUsuario>();
        servicios.AddScoped<IUnidadTrabajoIdentidad, UnidadTrabajoIdentidad>();

        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();

        servicios.Configure<OpcionesKeycloak>(configuracion.GetSection(OpcionesKeycloak.Seccion));
        servicios.AddHttpClient<IProveedorIdentidad, KeycloakProveedorIdentidad>();

        return servicios;
    }
}
