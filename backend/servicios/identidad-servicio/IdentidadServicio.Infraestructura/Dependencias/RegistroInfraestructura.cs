using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Infraestructura.Logging;
using IdentidadServicio.Infraestructura.Notificaciones;
using IdentidadServicio.Infraestructura.Persistencia;
using IdentidadServicio.Infraestructura.Persistencia.Repositorios;
using IdentidadServicio.Infraestructura.Seguridad;
using IdentidadServicio.Infraestructura.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        servicios.AddScoped<IRepositorioUsuariosLectura, RepositorioUsuariosLectura>();
        servicios.AddScoped<IRepositorioOperadores, RepositorioOperadores>();
        servicios.AddScoped<IRepositorioParticipantes, RepositorioParticipantes>();
        servicios.AddScoped<IRepositorioAdministradores, RepositorioAdministradores>();
        servicios.AddScoped<IRepositorioUnicidadUsuario, RepositorioUnicidadUsuario>();
        servicios.AddScoped<IRepositorioControlContrasenaTemporal, RepositorioControlContrasenaTemporal>();
        servicios.AddScoped<IUnidadTrabajoIdentidad, UnidadTrabajoIdentidad>();
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();
        servicios.Configure<OpcionesKeycloak>(configuracion.GetSection(OpcionesKeycloak.Seccion));
        servicios.AddHttpClient<IProveedorIdentidad, KeycloakProveedorIdentidad>();
        // --- Envío de correo -------------------------------------------------
        // El proveedor se elige por configuración (EnvioCorreo:Proveedor). SMTP es
        // el predeterminado (desarrollo local); GmailApi se activa en despliegue.
        // Ambas implementaciones se registran como clases resolubles y una ÚNICA
        // resolución de IServicioCorreo delega la selección en SelectorProveedorCorreo,
        // evitando registros ambiguos cuyo resultado dependa del orden.
        servicios.Configure<OpcionesCorreo>(configuracion.GetSection(OpcionesCorreo.Seccion));
        servicios.Configure<OpcionesEnvioCorreo>(configuracion.GetSection(OpcionesEnvioCorreo.Seccion));
        servicios.Configure<OpcionesGmailApi>(configuracion.GetSection(OpcionesGmailApi.Seccion));

        servicios.AddScoped<ServicioCorreoSmtp>();
        servicios.AddHttpClient<ServicioCorreoGmailApi>(cliente =>
        {
            cliente.Timeout = TimeSpan.FromSeconds(30);
        });

        servicios.AddScoped<IServicioCorreo>(proveedorServicios =>
            SelectorProveedorCorreo.Resolver(
                proveedorServicios.GetRequiredService<IOptions<OpcionesEnvioCorreo>>().Value.Proveedor,
                proveedorServicios.GetRequiredService<ServicioCorreoSmtp>,
                proveedorServicios.GetRequiredService<ServicioCorreoGmailApi>));

        servicios.AddScoped<IRegistroLogsAplicacion, RegistroLogsAplicacionDotNet>();

        return servicios;
    }
}
