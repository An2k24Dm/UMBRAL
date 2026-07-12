using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.Persistencia.Repositorios;
using RankingServicio.Infraestructura.RabbitMq;
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

        servicios.AddScoped<IRepositorioRankingParticipante, RepositorioRankingParticipante>();
        servicios.AddScoped<IRepositorioRankingEquipo, RepositorioRankingEquipo>();
        servicios.AddScoped<IRepositorioRankingGlobal, RepositorioRankingGlobal>();
        servicios.AddScoped<IRepositorioEventosProcesados, RepositorioEventosProcesados>();
        servicios.AddScoped<IUnidadTrabajoRanking, UnidadTrabajoRanking>();

        servicios.AddSingleton<INotificadorRankingTiempoReal, NotificadorRankingTiempoReal>();
        servicios.AddSingleton<IProveedorFechaHora, ProveedorFechaHoraUtc>();

        servicios.Configure<OpcionesRabbitMq>(
            configuracion.GetSection(OpcionesRabbitMq.Seccion));
        servicios.AddHostedService<ConsumidorEventosRanking>();

        return servicios;
    }
}
