using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Servicios;

public sealed class ServicioFinalizacionSesion : IServicioFinalizacionSesion
{
    private readonly IRepositorioSesiones _sesiones;
    private readonly IRepositorioEtapasCompletadas _etapasCompletadas;
    private readonly IClienteJuegosMisiones _clienteMisiones;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ServicioFinalizacionSesion(
        IRepositorioSesiones sesiones,
        IRepositorioEtapasCompletadas etapasCompletadas,
        IClienteJuegosMisiones clienteMisiones,
        INotificadorSesionesTiempoReal notificador,
        IUnidadTrabajoSesiones unidadTrabajo,
        IProveedorFechaHora reloj)
    {
        _sesiones = sesiones;
        _etapasCompletadas = etapasCompletadas;
        _clienteMisiones = clienteMisiones;
        _notificador = notificador;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
    }

    public async Task FinalizarSiTodasEtapasCompletadasAsync(
        Guid sesionId, Guid etapaIdCompletada, CancellationToken cancelacion)
    {
        // Registrar la etapa como completada (idempotente)
        await _etapasCompletadas.RegistrarAsync(
            sesionId, etapaIdCompletada, _reloj.ObtenerFechaHoraUtc(), cancelacion);

        var sesion = await _sesiones.ObtenerPorIdAsync(sesionId, cancelacion);
        if (sesion is null || sesion.Estado != EstadoSesion.Activa) return;

        // Sumar etapas totales llamando a juegos-servicio por cada misión
        var totalEtapas = 0;
        foreach (var mision in sesion.Misiones)
        {
            var detalle = await _clienteMisiones.ObtenerMisionConEtapasAsync(
                mision.MisionId, cancelacion);
            totalEtapas += detalle?.Etapas.Count ?? 0;
        }

        if (totalEtapas <= 0) return;

        var completadas = await _etapasCompletadas.ContarAsync(sesionId, cancelacion);
        if (completadas < totalEtapas) return;

        // Todas las etapas completadas → finalizar la sesión automáticamente
        sesion.Finalizar(_reloj.ObtenerFechaHoraUtc());
        await _sesiones.ActualizarAsync(sesion, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificador.NotificarSesionActualizadaAsync(
            sesion.Id, sesion.Estado.ToString(), cancelacion);
    }
}
