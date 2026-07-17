using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Aplicacion.Procesos.VencimientoEtapas;

public sealed class ProcesadorVencimientoEtapasSesion : IProcesadorVencimientosEtapas
{
    private readonly IConsultasSesiones _consultas;
    private readonly IServicioFinalizacionSesion _finalizacion;
    private readonly IProveedorFechaHora _reloj;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ProcesadorVencimientoEtapasSesion(
        IConsultasSesiones consultas,
        IServicioFinalizacionSesion finalizacion,
        IProveedorFechaHora reloj,
        IRegistroLogsAplicacion registroLogs)
    {
        _consultas = consultas;
        _finalizacion = finalizacion;
        _reloj = reloj;
        _registroLogs = registroLogs;
    }

    public async Task<int> EjecutarCicloAsync(CancellationToken cancelacion)
    {
        var procesadas = 0;
        procesadas += await FinalizarSesionesVencidasAsync(cancelacion);
        procesadas += await CerrarCierresPendientesVencidosAsync(cancelacion);
        procesadas += await ActivarPreparacionesVencidasAsync(cancelacion);
        procesadas += await CerrarEtapasVencidasAsync(cancelacion);
        return procesadas;
    }

    private async Task<int> FinalizarSesionesVencidasAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var vencidas = await _consultas.ListarActivasConDuracionVencidaAsync(ahoraUtc, cancelacion);
        if (vencidas.Count == 0) return 0;

        _registroLogs.Informacion(
            evento: "VencimientoSesiones",
            descripcion: "[VencimientoEtapas] sesiones activas con duración vencida encontradas.",
            propiedades: new Dictionary<string, object?>
            {
                ["Encontradas"] = vencidas.Count
            });

        var finalizadas = 0;
        foreach (var sesion in vencidas)
        {
            try
            {
                await _finalizacion.FinalizarSesionPorVencimientoAsync(
                    sesion.Id, cancelacion);
                finalizadas++;
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "VencimientoSesionFallido",
                    descripcion: "No se pudo finalizar la sesión vencida por tiempo.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["SesionId"] = sesion.Id
                    });
            }
        }

        return finalizadas;
    }

    private async Task<int> CerrarCierresPendientesVencidosAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var pendientes = await _consultas.ListarActivasConCierrePendienteVencidoAsync(ahoraUtc, cancelacion);
        if (pendientes.Count == 0) return 0;

        _registroLogs.Informacion(
            evento: "VencimientoEtapasCierresPendientes",
            descripcion: "[VencimientoEtapas] cierres pendientes vencidos encontrados.",
            propiedades: new Dictionary<string, object?>
            {
                ["Encontrados"] = pendientes.Count
            });

        var cerradas = 0;
        foreach (var sesion in pendientes)
        {
            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is null) continue;

            _registroLogs.Depuracion(
                evento: "TransicionCierrePendienteVencido",
                descripcion: "[Transicion] CierrePendiente vencido; se cierra la etapa.",
                propiedades: new Dictionary<string, object?>
                {
                    ["SesionId"] = sesion.Id,
                    ["EtapaId"] = ejecucion.EtapaId
                });
            try
            {
                await _finalizacion.CerrarEtapaTrasCierrePendienteAsync(
                    sesion.Id, ejecucion.EtapaId, cancelacion);
                cerradas++;
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "CierrePendienteFallido",
                    descripcion: "No se pudo cerrar la etapa tras el feedback final.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["SesionId"] = sesion.Id,
                        ["EtapaId"] = ejecucion.EtapaId
                    });
            }
        }

        return cerradas;
    }

    private async Task<int> ActivarPreparacionesVencidasAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var preparadas = await _consultas.ListarActivasConPreparacionVencidaAsync(ahoraUtc, cancelacion);
        if (preparadas.Count == 0) return 0;

        _registroLogs.Informacion(
            evento: "VencimientoEtapasPreparaciones",
            descripcion: "[VencimientoEtapas] preparaciones vencidas encontradas (se activarán).",
            propiedades: new Dictionary<string, object?>
            {
                ["Encontradas"] = preparadas.Count
            });

        var activadas = 0;
        foreach (var sesion in preparadas)
        {
            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is null) continue;

            _registroLogs.Depuracion(
                evento: "TransicionPreparacionVencida",
                descripcion: "[Transicion] Preparacion vencida; se activa la etapa (EtapaIniciada).",
                propiedades: new Dictionary<string, object?>
                {
                    ["SesionId"] = sesion.Id,
                    ["EtapaId"] = ejecucion.EtapaId
                });
            try
            {
                await _finalizacion.ActivarEtapaProgramadaAsync(
                    sesion.Id, ejecucion.EtapaId, cancelacion);
                activadas++;
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "ActivacionEtapaProgramadaFallida",
                    descripcion: "No se pudo activar la etapa programada.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["SesionId"] = sesion.Id,
                        ["EtapaId"] = ejecucion.EtapaId
                    });
            }
        }

        return activadas;
    }

    private async Task<int> CerrarEtapasVencidasAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var vencidas = await _consultas.ListarActivasConEtapaVencidaAsync(ahoraUtc, cancelacion);
        if (vencidas.Count == 0) return 0;

        var procesadas = 0;
        foreach (var sesion in vencidas)
        {
            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is null) continue;

            try
            {
                await _finalizacion.AvanzarEtapaPorVencimientoAsync(
                    sesion.Id, ejecucion.EtapaId, cancelacion);
                procesadas++;
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "VencimientoEtapaFallido",
                    descripcion: "No se pudo cerrar la etapa vencida por tiempo.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["SesionId"] = sesion.Id,
                        ["EtapaId"] = ejecucion.EtapaId
                    });
            }
        }

        return procesadas;
    }
}
