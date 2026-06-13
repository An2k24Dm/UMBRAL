using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.ServiciosEnSegundoPlano;
using SesionesServicio.Infraestructura.Mapeadores;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;
using SesionesServicio.Infraestructura.ServiciosEnSegundoPlano;
using SesionesServicio.Infraestructura.ServiciosExternos;
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

        // Strategy de mapeo de persistencia por tipo de sesión. Stateless;
        // agregar un tipo nuevo solo requiere registrar su estrategia.
        servicios.AddSingleton<IMapeadorPersistenciaSesion, MapeadorPersistenciaSesionIndividual>();
        servicios.AddSingleton<IMapeadorPersistenciaSesion, MapeadorPersistenciaSesionGrupal>();
        servicios.AddSingleton<MapeadorSesionesPersistencia>();

        // RepositorioSesiones implementa el puerto del agregado (dominio) y el
        // puerto de consultas (aplicación). Registramos una sola instancia por
        // alcance y la reexponemos en ambos contratos para que compartan el
        // mismo DbContext y unidad de trabajo.
        servicios.AddScoped<RepositorioSesiones>();
        servicios.AddScoped<Dominio.Abstract.IRepositorioSesiones>(
            sp => sp.GetRequiredService<RepositorioSesiones>());
        servicios.AddScoped<IConsultasSesiones>(
            sp => sp.GetRequiredService<RepositorioSesiones>());
        servicios.AddScoped<IUnidadTrabajoSesiones, UnidadTrabajoSesiones>();

        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraSistema>();
        servicios.AddSingleton<IGeneradorCodigoAcceso, GeneradorCodigoAccesoAleatorio>();

        var configMapster = TypeAdapterConfig.GlobalSettings;
        ConfiguracionMapsterSesiones.Configurar(configMapster);
        servicios.AddSingleton(configMapster);

        servicios.Configure<OpcionesJuegosServicio>(
            configuracion.GetSection(OpcionesJuegosServicio.Seccion));
        servicios.Configure<OpcionesIdentidadServicio>(
            configuracion.GetSection(OpcionesIdentidadServicio.Seccion));

        servicios.AddHttpClient<IClienteJuegosMisiones, ClienteJuegosMisionesHttp>();
        servicios.AddHttpClient<IClienteIdentidadUsuarios, ClienteIdentidadUsuariosHttp>();

        servicios.Configure<OpcionesPreparacionSesiones>(
            configuracion.GetSection(OpcionesPreparacionSesiones.Seccion));
        servicios.AddScoped<ProcesadorPreparacionSesiones>();
        servicios.AddHostedService<ServicioPreparacionSesionesProgramadas>();

        return servicios;
    }
}
