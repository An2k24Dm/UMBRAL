using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SesionesServicio.Aplicacion.Procesos.FinalizacionSesionesPorTiempo;

namespace SesionesServicio.Infraestructura.ServiciosEnSegundoPlano;

public sealed class ServicioFinalizacionSesionesPorTiempo : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _fabricaAlcances;
    private readonly ILogger<ServicioFinalizacionSesionesPorTiempo> _registro;

    public ServicioFinalizacionSesionesPorTiempo(
        IServiceScopeFactory fabricaAlcances,
        ILogger<ServicioFinalizacionSesionesPorTiempo> registro)
    {
        _fabricaAlcances = fabricaAlcances;
        _registro = registro;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        _registro.LogInformation("ServicioFinalizacionSesionesPorTiempo iniciado.");

        while (!cancelacion.IsCancellationRequested)
        {
            try
            {
                await using var alcance = _fabricaAlcances.CreateAsyncScope();
                var procesador = alcance.ServiceProvider
                    .GetRequiredService<ProcesadorFinalizacionSesionesPorTiempo>();
                await procesador.EjecutarCicloAsync(cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _registro.LogError(ex,
                    "Error no controlado en el ciclo de finalización de sesiones por tiempo.");
            }

            try
            {
                await Task.Delay(Intervalo, cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _registro.LogInformation("ServicioFinalizacionSesionesPorTiempo detenido.");
    }
}
