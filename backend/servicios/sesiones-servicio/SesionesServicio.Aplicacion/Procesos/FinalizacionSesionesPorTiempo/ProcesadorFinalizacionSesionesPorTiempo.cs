using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Procesos.FinalizacionSesionesPorTiempo;

public sealed class ProcesadorFinalizacionSesionesPorTiempo
{
    private readonly IConsultasSesiones _consultas;
    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ProcesadorFinalizacionSesionesPorTiempo(
        IConsultasSesiones consultas,
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IProveedorFechaHora reloj,
        INotificadorSesionesTiempoReal notificador,
        IRegistroLogsAplicacion registroLogs)
    {
        _consultas = consultas;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
        _notificador = notificador;
        _registroLogs = registroLogs;
    }

    public async Task<int> EjecutarCicloAsync(CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var expiradas = await _consultas.ListarActivasConTiempoVencidoAsync(ahoraUtc, cancelacion);

        if (expiradas.Count == 0) return 0;

        _registroLogs.Informacion(
            evento: "FinalizacionPorTiempoDetectadas",
            descripcion: "Sesiones activas con tiempo de juego vencido.",
            propiedades: new Dictionary<string, object?> { ["Cantidad"] = expiradas.Count });

        var sesionesFinalizadas = new List<Guid>();
        foreach (var sesion in expiradas)
        {
            if (sesion.Estado != EstadoSesion.Activa) continue;

            try
            {
                sesion.Finalizar(ahoraUtc);
                await _repositorio.ActualizarAsync(sesion, cancelacion);
                sesionesFinalizadas.Add(sesion.Id);
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "FinalizacionPorTiempoFallida",
                    descripcion: "No se pudo finalizar la sesión por tiempo vencido.",
                    propiedades: new Dictionary<string, object?> { ["SesionId"] = sesion.Id });
            }
        }

        if (sesionesFinalizadas.Count > 0)
        {
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

            foreach (var sesionId in sesionesFinalizadas)
            {
                try
                {
                    await _notificador.NotificarSesionActualizadaAsync(
                        sesionId, EstadoSesion.Finalizada.ToString(), cancelacion);
                }
                catch (Exception ex)
                {
                    _registroLogs.Error(
                        excepcion: ex,
                        evento: "FinalizacionPorTiempoNotificacionFallida",
                        descripcion: "No se pudo notificar la finalización por tiempo.",
                        propiedades: new Dictionary<string, object?> { ["SesionId"] = sesionId });
                }
            }
        }

        _registroLogs.Informacion(
            evento: "FinalizacionPorTiempoCicloCompletado",
            descripcion: "Ciclo de finalización por tiempo completado.",
            propiedades: new Dictionary<string, object?>
            {
                ["Finalizadas"] = sesionesFinalizadas.Count,
                ["Total"] = expiradas.Count
            });

        return sesionesFinalizadas.Count;
    }
}
