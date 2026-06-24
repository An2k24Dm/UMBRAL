using Microsoft.Extensions.Logging;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Procesos.PreparacionSesiones;

// HU34/5.1 — Núcleo de la transición automática Programada → EnPreparacion.
//
// Se separa del HostedService para que se pueda ejecutar fuera de un
// host (por ejemplo desde pruebas unitarias). El HostedService se
// limita a llamar a EjecutarCicloAsync en un timer.
//
// El servicio NO toca el estado directamente: rehidrata el agregado,
// llama a sesion.Preparar() (patrón State) y persiste el cambio. Si
// otra ejecución concurrente ya cambió el estado, la comparación a
// Programada antes de llamar a Preparar() evita transiciones inválidas.
public sealed class ProcesadorPreparacionSesiones
{
    // Lectura (listado de candidatas) por el puerto de consultas; escritura
    // (persistir la transición de estado) por el repositorio del agregado.
    private readonly IConsultasSesiones _consultas;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ProcesadorPreparacionSesiones> _registro;

    public ProcesadorPreparacionSesiones(
        IConsultasSesiones consultas,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IProveedorFechaHora reloj,
        ILogger<ProcesadorPreparacionSesiones> registro)
    {
        _consultas = consultas;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<ResultadoCiclo> EjecutarCicloAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var candidatas = await _consultas.ListarProgramadasVencidasAsync(ahoraUtc, cancelacion);

        if (candidatas.Count == 0)
        {
            _registro.LogDebug(
                "No hay sesiones Programadas vencidas a las {AhoraUtc:o}.", ahoraUtc);
            return new ResultadoCiclo(0, 0);
        }

        _registro.LogInformation(
            "Detectadas {Cantidad} sesiones Programadas vencidas.", candidatas.Count);

        var preparadas = 0;
        foreach (var sesion in candidatas)
        {
            if (sesion.Estado != EstadoSesion.Programada)
                continue;

            try
            {
                sesion.Preparar();
                await _repositorio.ActualizarAsync(sesion, cancelacion);
                preparadas++;
            }
            catch (Exception ex)
            {
                _registro.LogError(ex,
                    "No se pudo preparar la sesión {SesionId}.", sesion.Id);
            }
        }

        if (preparadas > 0)
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Ciclo de preparación completado: {Preparadas}/{Total} sesiones pasaron a EnPreparacion.",
            preparadas, candidatas.Count);

        return new ResultadoCiclo(candidatas.Count, preparadas);
    }

    public readonly record struct ResultadoCiclo(int Encontradas, int Preparadas);
}
