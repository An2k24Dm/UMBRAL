using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Infraestructura.Logging;
using PartidasServicio.Infraestructura.Persistencia;
using PartidasServicio.Infraestructura.Persistencia.Repositorios;
using PartidasServicio.Infraestructura.ServiciosExternos;
using PartidasServicio.Infraestructura.Tiempo;
using PartidasServicio.Infraestructura.TiempoReal;
using PartidasServicio.Infraestructura.TiempoReal.Hubs;

namespace PartidasServicio.Infraestructura.Dependencias;

public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("BaseDatos")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'BaseDatos'.");

        servicios.AddDbContext<ContextoPartidas>(opciones =>
            opciones.UseNpgsql(cadenaConexion, p =>
            {
                p.MigrationsHistoryTable("__historial_migraciones", "partidas");
                p.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        servicios.AddScoped<IUnidadTrabajoPartidas>(
            sp => sp.GetRequiredService<ContextoPartidas>());

        servicios.AddScoped<RepositorioRespuestas>();
        servicios.AddScoped<IRepositorioRespuestas>(sp => sp.GetRequiredService<RepositorioRespuestas>());
        servicios.AddScoped<IConsultasPartidas>(sp => sp.GetRequiredService<RepositorioRespuestas>());

        servicios.AddScoped<IRepositorioPartidas, RepositorioPartidas>();

        servicios.AddScoped<INotificadorPartidasTiempoReal, NotificadorPartidasTiempoReal>();
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();
        servicios.AddSingleton<IUserIdProvider, ProveedorIdUsuarioSignalR>();

        servicios.Configure<OpcionesSesionesServicio>(
            configuracion.GetSection("ServiciosExternos:SesionesServicio"));
        servicios.Configure<OpcionesJuegosServicio>(
            configuracion.GetSection("ServiciosExternos:JuegosServicio"));

        servicios.AddHttpClient<IClienteSesiones, ClienteSesionesHttp>();
        servicios.AddHttpClient<IClienteJuegos, ClienteJuegosHttp>();

        servicios.AddScoped<IRegistroLogsAplicacion, RegistroLogsAplicacionDotNet>();

        return servicios;
    }
}
