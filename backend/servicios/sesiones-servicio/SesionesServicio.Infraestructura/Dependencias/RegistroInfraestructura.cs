using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.Procesos.FinalizacionSesionesPorTiempo;
using SesionesServicio.Aplicacion.Procesos.PreparacionSesiones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.Configuraciones;
using SesionesServicio.Infraestructura.Logging;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;
using SesionesServicio.Infraestructura.ServiciosEnSegundoPlano;
using SesionesServicio.Infraestructura.Seguridad;
using SesionesServicio.Infraestructura.ServiciosExternos;
using SesionesServicio.Infraestructura.TiempoReal;
using SesionesServicio.Infraestructura.Tiempo;

namespace SesionesServicio.Infraestructura.Dependencias;

public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaConexion = configuracion.GetConnectionString("BaseDatos")
                             ?? throw new InvalidOperationException(
                                 "Falta la cadena de conexión 'BaseDatos'.");
        servicios.AddDbContext<ContextoSesiones>(opciones =>
            opciones.UseNpgsql(cadenaConexion, p =>
            {
                p.MigrationsHistoryTable("__historial_migraciones", "sesiones");
                p.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));
        servicios.AddSingleton<IMapeadorPersistenciaSesion, MapeadorPersistenciaSesionIndividual>();
        servicios.AddSingleton<IMapeadorPersistenciaSesion, MapeadorPersistenciaSesionGrupal>();
        servicios.AddSingleton<MapeadorSesionesPersistencia>();
        servicios.AddScoped<RepositorioSesiones>();
        servicios.AddScoped<Dominio.Abstract.IRepositorioSesiones>(
            sp => sp.GetRequiredService<RepositorioSesiones>());
        servicios.AddScoped<IConsultasSesiones>(
            sp => sp.GetRequiredService<RepositorioSesiones>());
        servicios.AddScoped<IUnidadTrabajoSesiones, UnidadTrabajoSesiones>();
        servicios.AddScoped<INotificadorSesionesTiempoReal, NotificadorSesionesTiempoReal>();
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();
        servicios.AddSingleton<IGeneradorCodigoAcceso, GeneradorCodigoAccesoAleatorio>();
        servicios.AddSingleton<IHashContrasenaEquipo, HashContrasenaEquipo>();
        var configMapster = TypeAdapterConfig.GlobalSettings;
        ConfiguracionMapsterSesiones.Configurar(configMapster);
        servicios.AddSingleton(configMapster);
        servicios.Configure<OpcionesJuegosServicio>(
            configuracion.GetSection(OpcionesJuegosServicio.Seccion));
        servicios.Configure<OpcionesIdentidadServicio>(
            configuracion.GetSection(OpcionesIdentidadServicio.Seccion));
        servicios.AddHttpClient<IClienteJuegosMisiones, ClienteJuegosMisionesHttp>();
        servicios.AddHttpClient<IClienteJuegosTrivia, ClienteJuegosTriviaHttp>();
        servicios.AddHttpClient<IClienteBusquedaTesoro, ClienteBusquedaTesoroHttp>();
        servicios.AddHttpClient<IClienteIdentidadUsuarios, ClienteIdentidadUsuariosHttp>();
        servicios.AddHttpClient<IClienteIdentidadParticipantes, ClienteIdentidadParticipantes>();
        servicios.AddScoped<IRepositorioRespuestasTrivia, RepositorioRespuestasTrivia>();
        servicios.AddScoped<IRepositorioEvidenciasTesoro, RepositorioEvidenciasTesoro>();
        servicios.AddScoped<IRepositorioPistasLiberadas, RepositorioPistasLiberadas>();
        servicios.AddScoped<IRepositorioEtapasCompletadas, RepositorioEtapasCompletadas>();
        servicios.Configure<OpcionesPreparacionSesiones>(
            configuracion.GetSection(OpcionesPreparacionSesiones.Seccion));
        servicios.AddScoped<ProcesadorPreparacionSesiones>();
        servicios.AddHostedService<ServicioPreparacionSesionesProgramadas>();
        servicios.AddScoped<ProcesadorFinalizacionSesionesPorTiempo>();
        servicios.AddHostedService<ServicioFinalizacionSesionesPorTiempo>();
        servicios.AddScoped<IRegistroLogsAplicacion, RegistroLogsAplicacionDotNet>();
        return servicios;
    }
}
