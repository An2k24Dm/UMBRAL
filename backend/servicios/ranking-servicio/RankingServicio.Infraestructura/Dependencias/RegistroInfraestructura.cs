using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.Persistencia.Consultas;
using RankingServicio.Infraestructura.Persistencia.Repositorios;
using RankingServicio.Infraestructura.RabbitMq;
using RankingServicio.Infraestructura.ServiciosExternos;
using RankingServicio.Infraestructura.Tiempo;
using RankingServicio.Infraestructura.TiempoReal;

namespace RankingServicio.Infraestructura.Dependencias;

public static class RegistroInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddDbContext<ContextoRanking>(opc =>
        {
            opc.UseNpgsql(
                configuracion.GetConnectionString("BaseDatos"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__historial_migraciones", "ranking");
                    npgsql.EnableRetryOnFailure(5);
                });
        });

        servicios.AddScoped<IRepositorioRanking, RepositorioRanking>();
        servicios.AddScoped<IConsultasRanking, ConsultasRanking>();
        servicios.AddScoped<IRepositorioEventosProcesados, RepositorioEventosProcesados>();
        servicios.AddScoped<IUnidadTrabajoRanking, UnidadTrabajoRanking>();

        servicios.AddSingleton<INotificadorRankingTiempoReal, NotificadorRankingTiempoReal>();
        servicios.AddScoped<IPublicadorResultadosPuntaje, PublicadorResultadosPuntajeOutbox>();
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraUtc>();

        // Clientes HTTP para enriquecer alias y nombres de equipo al consultar.
        servicios.Configure<OpcionesIdentidadServicio>(
            configuracion.GetSection(OpcionesIdentidadServicio.Seccion));
        servicios.Configure<OpcionesSesionesServicio>(
            configuracion.GetSection(OpcionesSesionesServicio.Seccion));
        servicios.AddHttpClient<IClienteIdentidadParticipantes, ClienteIdentidadParticipantesRanking>();
        servicios.AddHttpClient<IClienteSesionesRanking, ClienteSesionesRankingHttp>();

        servicios.Configure<OpcionesRabbitMq>(
            configuracion.GetSection(OpcionesRabbitMq.Seccion));
        servicios.AddHostedService<ConsumidorEventosRanking>();
        servicios.AddHostedService<DespachadorOutboxResultadosRabbitMq>();

        return servicios;
    }
}
