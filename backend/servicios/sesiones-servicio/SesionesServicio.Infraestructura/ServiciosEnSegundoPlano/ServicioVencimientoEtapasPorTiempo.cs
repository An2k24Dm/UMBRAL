using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.Configuraciones;

namespace SesionesServicio.Infraestructura.ServiciosEnSegundoPlano;

public sealed class ServicioVencimientoEtapasPorTiempo : BackgroundService
{
    private readonly IServiceScopeFactory _fabricaAlcances;
    private readonly IOptionsMonitor<OpcionesVencimientoEtapas> _opciones;
    private readonly ILogger<ServicioVencimientoEtapasPorTiempo> _registro;

    public ServicioVencimientoEtapasPorTiempo(
        IServiceScopeFactory fabricaAlcances,
        IOptionsMonitor<OpcionesVencimientoEtapas> opciones,
        ILogger<ServicioVencimientoEtapasPorTiempo> registro)
    {
        _fabricaAlcances = fabricaAlcances;
        _opciones = opciones;
        _registro = registro;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        _registro.LogInformation("ServicioVencimientoEtapasPorTiempo iniciado.");

        while (!cancelacion.IsCancellationRequested)
        {
            try
            {
                await using var alcance = _fabricaAlcances.CreateAsyncScope();
                var procesador = alcance.ServiceProvider
                    .GetRequiredService<IProcesadorVencimientosEtapas>();
                await procesador.EjecutarCicloAsync(cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _registro.LogError(ex,
                    "Error no controlado en el ciclo de vencimiento de etapas.");
            }

            var segundos = Math.Max(1, _opciones.CurrentValue.IntervaloVencimientoSegundos);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(segundos), cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _registro.LogInformation("ServicioVencimientoEtapasPorTiempo detenido.");
    }
}
