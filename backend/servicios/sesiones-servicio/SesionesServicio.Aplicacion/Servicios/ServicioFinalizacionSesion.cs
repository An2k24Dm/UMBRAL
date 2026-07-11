using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Servicios;

public sealed class ServicioFinalizacionSesion : IServicioFinalizacionSesion
{
    public const int DuracionPreparacionEntreEtapasSegundos = 10;

    public const int DuracionFeedbackFinalSegundos = 5;

    private enum DisparadorCierre
    {
        TodosCompletaron,  // Activa → cierre inmediato (uso directo/tests).
        VencimientoActiva, // Activa vencida por tiempo global.
        CierrePendiente    // Feedback final vencido: cerrar realmente.
    }

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

    public Task FinalizarSiTodasEtapasCompletadasAsync(
        Guid sesionId, Guid etapaIdCompletada, CancellationToken cancelacion)
        => CerrarEtapaYTransicionarAsync(
            sesionId, etapaIdCompletada, DisparadorCierre.TodosCompletaron, cancelacion);

    public Task AvanzarEtapaPorVencimientoAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
        => CerrarEtapaYTransicionarAsync(
            sesionId, etapaId, DisparadorCierre.VencimientoActiva, cancelacion);

    public Task CerrarEtapaTrasCierrePendienteAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
        => CerrarEtapaYTransicionarAsync(
            sesionId, etapaId, DisparadorCierre.CierrePendiente, cancelacion);

    public async Task ProgramarCierreTrasFeedbackAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var sesion = await _sesiones.ObtenerPorIdAsync(sesionId, ct);
            if (sesion is null || sesion.Estado != EstadoSesion.Activa) return;
            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is null || ejecucion.EtapaId != etapaId) return;

            if (!ejecucion.EstaActiva) return;

            sesion.ProgramarCierrePendiente(etapaId, ahoraUtc, DuracionFeedbackFinalSegundos);
            await _sesiones.ActualizarAsync(sesion, ct);
            await _unidadTrabajo.GuardarCambiosAsync(ct);
        }, cancelacion);
    }

    private async Task CerrarEtapaYTransicionarAsync(
        Guid sesionId, Guid etapaIdCompletada, DisparadorCierre disparador,
        CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        ResultadoTransicion? resultado = null;

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var sesion = await _sesiones.ObtenerPorIdAsync(sesionId, ct);
            if (sesion is null || sesion.Estado != EstadoSesion.Activa) return;
            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is null || ejecucion.EtapaId != etapaIdCompletada) return;

            var puedeCerrar = disparador switch
            {
                DisparadorCierre.TodosCompletaron => ejecucion.EstaActiva,
                DisparadorCierre.VencimientoActiva =>
                    ejecucion.EstaActiva && ejecucion.CalcularSegundosRestantes(ahoraUtc) <= 0,
                DisparadorCierre.CierrePendiente =>
                    ejecucion.EstaEnCierrePendiente && ejecucion.CierrePendienteVencido(ahoraUtc),
                _ => false
            };
            if (!puedeCerrar) return;

            _ = await _etapasCompletadas.RegistrarAsync(
                sesionId, etapaIdCompletada, ahoraUtc, ct);

            var secuencia = await ConstruirSecuenciaAsync(sesion, ct);
            if (secuencia.Count == 0) return;

            var indiceActual = secuencia.FindIndex(e => e.EtapaId == etapaIdCompletada);
            if (indiceActual < 0) return;
            var etapaActual = secuencia[indiceActual];

            if (indiceActual == secuencia.Count - 1)
            {
                sesion.CompletarUltimaEtapa(etapaIdCompletada);
                sesion.Finalizar(ahoraUtc);
                await _sesiones.ActualizarAsync(sesion, ct);
                await _unidadTrabajo.GuardarCambiosAsync(ct);
                resultado = ResultadoTransicion.Finalizada(sesion.Id, etapaActual.MisionId);
                return;
            }

            var siguiente = secuencia[indiceActual + 1];
            var esNuevaMision = siguiente.MisionId != etapaActual.MisionId;
            var fechaInicioProgramadaUtc =
                ahoraUtc.AddSeconds(DuracionPreparacionEntreEtapasSegundos);

            sesion.ProgramarSiguienteEtapa(
                etapaIdCompletada,
                siguiente,
                ahoraUtc,
                DuracionPreparacionEntreEtapasSegundos);

            await _sesiones.ActualizarAsync(sesion, ct);
            await _unidadTrabajo.GuardarCambiosAsync(ct);
            resultado = ResultadoTransicion.Programada(
                sesion.Id, etapaActual.MisionId, siguiente,
                esNuevaMision, fechaInicioProgramadaUtc);
        }, cancelacion);

        if (resultado is null) return;

        await _notificador.NotificarEtapaCompletadaAsync(
            sesionId, resultado.MisionIdCompletada, etapaIdCompletada, cancelacion);

        if (resultado.Siguiente is null)
        {
            await _notificador.NotificarSesionActualizadaAsync(
                sesionId, EstadoSesion.Finalizada.ToString(), cancelacion);
            return;
        }

        await _notificador.NotificarEtapaPorComenzarAsync(
            sesionId,
            resultado.Siguiente.MisionId,
            resultado.Siguiente.EtapaId,
            resultado.Siguiente.TipoEtapa,
            resultado.Siguiente.ModoDeJuegoId,
            resultado.Siguiente.OrdenMision,
            resultado.Siguiente.OrdenEtapa,
            resultado.Siguiente.OrdenGlobal,
            resultado.EsNuevaMision,
            resultado.FechaInicioProgramadaUtc,
            DuracionPreparacionEntreEtapasSegundos,
            cancelacion);
    }

    public async Task ActivarEtapaProgramadaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        EjecucionActualSesion? activada = null;

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var sesion = await _sesiones.ObtenerPorIdAsync(sesionId, ct);
            if (sesion is null || sesion.Estado != EstadoSesion.Activa) return;
            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is null || ejecucion.EtapaId != etapaId) return;

            if (!ejecucion.EstaEnPreparacion || !ejecucion.PreparacionVencida(ahoraUtc))
                return;

            sesion.ActivarEtapaProgramada(etapaId, ahoraUtc);
            await _sesiones.ActualizarAsync(sesion, ct);
            await _unidadTrabajo.GuardarCambiosAsync(ct);

            activada = sesion.EjecucionActual;
        }, cancelacion);

        if (activada is null) return;

        await _notificador.NotificarEtapaIniciadaAsync(
            sesionId,
            activada.MisionId,
            activada.EtapaId,
            activada.TipoEtapa,
            activada.ModoDeJuegoId,
            activada.OrdenGlobal,
            ahoraUtc,
            activada.DuracionSegundos,
            cancelacion);
    }

    private async Task<List<EjecucionActualSesion>> ConstruirSecuenciaAsync(
        Sesion sesion,
        CancellationToken cancelacion)
    {
        if (sesion.SecuenciaEtapas.Count > 0)
        {
            return sesion.SecuenciaEtapas
                .OrderBy(e => e.OrdenGlobal)
                .ToList();
        }

        var resultado = new List<EjecucionActualSesion>();
        var ordenGlobal = 1;

        foreach (var sesionMision in sesion.Misiones.OrderBy(m => m.Orden))
        {
            var mision = await _clienteMisiones.ObtenerMisionConEtapasAsync(
                sesionMision.MisionId, cancelacion);
            if (mision is null) continue;

            var ordenEtapa = 1;
            foreach (var etapa in mision.Etapas.OrderBy(e => e.Orden))
            {
                resultado.Add(EjecucionActualSesion.Planificar(
                    sesionMision.MisionId,
                    etapa.Id,
                    etapa.ModoDeJuegoId,
                    etapa.TipoModoDeJuego,
                    ordenGlobal++,
                    sesionMision.Orden,
                    ordenEtapa++,
                    etapa.TiempoEstimado));
            }
        }

        return resultado;
    }

    private sealed record ResultadoTransicion(
        Guid SesionId,
        Guid MisionIdCompletada,
        EjecucionActualSesion? Siguiente,
        bool EsNuevaMision,
        DateTime FechaInicioProgramadaUtc)
    {
        public static ResultadoTransicion Programada(
            Guid sesionId,
            Guid misionIdCompletada,
            EjecucionActualSesion siguiente,
            bool esNuevaMision,
            DateTime fechaInicioProgramadaUtc)
            => new(sesionId, misionIdCompletada, siguiente, esNuevaMision, fechaInicioProgramadaUtc);

        public static ResultadoTransicion Finalizada(Guid sesionId, Guid misionIdCompletada)
            => new(sesionId, misionIdCompletada, null, false, DateTime.MinValue);
    }
}
