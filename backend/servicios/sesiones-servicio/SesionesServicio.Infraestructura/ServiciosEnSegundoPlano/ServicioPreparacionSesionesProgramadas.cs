using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SesionesServicio.Aplicacion.ServiciosEnSegundoPlano;

namespace SesionesServicio.Infraestructura.ServiciosEnSegundoPlano;

// HU34/5.1 — HostedService que dispara periódicamente el
// ProcesadorPreparacionSesiones para que las sesiones cuya
// FechaProgramada ya venció pasen de Programada a EnPreparacion.
//
// El servicio vive dentro de sesiones-servicio (no hay contenedor
// aparte). En cada ciclo crea su propio IServiceScope para resolver
// DbContext y repositorios sin filtrar instancias entre iteraciones.
public sealed class ServicioPreparacionSesionesProgramadas : BackgroundService
{
    private readonly IServiceScopeFactory _fabricaAlcances;
    private readonly IOptionsMonitor<OpcionesPreparacionSesiones> _opciones;
    private readonly ILogger<ServicioPreparacionSesionesProgramadas> _registro;

    public ServicioPreparacionSesionesProgramadas(
        IServiceScopeFactory fabricaAlcances,
        IOptionsMonitor<OpcionesPreparacionSesiones> opciones,
        ILogger<ServicioPreparacionSesionesProgramadas> registro)
    {
        _fabricaAlcances = fabricaAlcances;
        _opciones = opciones;
        _registro = registro;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        _registro.LogInformation("ServicioPreparacionSesionesProgramadas iniciado.");

        while (!cancelacion.IsCancellationRequested)
        {
            try
            {
                await using var alcance = _fabricaAlcances.CreateAsyncScope();
                var procesador = alcance.ServiceProvider
                    .GetRequiredService<ProcesadorPreparacionSesiones>();
                await procesador.EjecutarCicloAsync(cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _registro.LogError(ex,
                    "Error no controlado en el ciclo de preparación de sesiones.");
            }

            var segundos = Math.Max(1, _opciones.CurrentValue.IntervaloPreparacionSegundos);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(segundos), cancelacion);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _registro.LogInformation("ServicioPreparacionSesionesProgramadas detenido.");
    }
}
