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
    private readonly IRepositorioEvidenciasTesoro _evidenciasTesoro;
    private readonly IClienteJuegosMisiones _clienteMisiones;
    private readonly IClienteBusquedaTesoro _clienteTesoro;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IPublicadorEventosRanking _publicadorRanking;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ServicioFinalizacionSesion(
        IRepositorioSesiones sesiones,
        IRepositorioEtapasCompletadas etapasCompletadas,
        IRepositorioEvidenciasTesoro evidenciasTesoro,
        IClienteJuegosMisiones clienteMisiones,
        IClienteBusquedaTesoro clienteTesoro,
        INotificadorSesionesTiempoReal notificador,
        IPublicadorEventosRanking publicadorRanking,
        IUnidadTrabajoSesiones unidadTrabajo,
        IProveedorFechaHora reloj)
    {
        _sesiones = sesiones;
        _etapasCompletadas = etapasCompletadas;
        _evidenciasTesoro = evidenciasTesoro;
        _clienteMisiones = clienteMisiones;
        _clienteTesoro = clienteTesoro;
        _notificador = notificador;
        _publicadorRanking = publicadorRanking;
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

    public async Task FinalizarSesionPorVencimientoAsync(
        Guid sesionId, CancellationToken cancelacion)
        => await FinalizarSesionPorVencimientoInternoAsync(
            sesionId, revalidarDuracion: false, cancelacion);

    public async Task<bool> FinalizarSesionSiDuracionVencidaAsync(
        Guid sesionId, CancellationToken cancelacion)
        => await FinalizarSesionPorVencimientoInternoAsync(
            sesionId, revalidarDuracion: true, cancelacion);

    private async Task<bool> FinalizarSesionPorVencimientoInternoAsync(
        Guid sesionId,
        bool revalidarDuracion,
        CancellationToken cancelacion)
    {
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var progresosRegistrados = new List<ProgresoRegistrado>();
        var finalizada = false;

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var sesion = await _sesiones.ObtenerPorIdAsync(sesionId, ct);
            if (sesion is null || sesion.Estado != EstadoSesion.Activa) return;
            if (revalidarDuracion && !SesionVencioPorDuracion(sesion, ahoraUtc)) return;

            var ejecucion = sesion.EjecucionActual;
            if (ejecucion is not null && ejecucion.EstaActiva)
            {
                progresosRegistrados.AddRange(
                    await RegistrarCerosTesoroSinEvidenciaAsync(sesion, ejecucion, ahoraUtc, ct));
            }

            sesion.Finalizar(ahoraUtc);
            await _sesiones.ActualizarAsync(sesion, ct);
            await _unidadTrabajo.GuardarCambiosAsync(ct);
            finalizada = true;
        }, cancelacion);

        if (!finalizada) return false;

        foreach (var progreso in progresosRegistrados)
        {
            await _notificador.NotificarProgresoSecuencialActualizadoAsync(
                sesionId,
                progreso.ParticipanteIdentidadId,
                progreso.EquipoId,
                cancelacion);
        }

        await _notificador.NotificarSesionActualizadaAsync(
            sesionId, EstadoSesion.Finalizada.ToString(), cancelacion);

        return true;
    }

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
        var progresosRegistrados = new List<ProgresoRegistrado>();

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

            if (disparador == DisparadorCierre.VencimientoActiva)
            {
                progresosRegistrados.AddRange(
                    await RegistrarCerosTesoroSinEvidenciaAsync(sesion, ejecucion, ahoraUtc, ct));
            }

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

        foreach (var progreso in progresosRegistrados)
        {
            await _notificador.NotificarProgresoSecuencialActualizadoAsync(
                sesionId,
                progreso.ParticipanteIdentidadId,
                progreso.EquipoId,
                cancelacion);
        }

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

    private async Task<List<ProgresoRegistrado>> RegistrarCerosTesoroSinEvidenciaAsync(
        Sesion sesion,
        EjecucionActualSesion ejecucion,
        DateTime ahoraUtc,
        CancellationToken cancelacion)
    {
        var registrados = new List<ProgresoRegistrado>();

        if (!string.Equals(ejecucion.TipoEtapa, "BusquedaTesoro", StringComparison.OrdinalIgnoreCase))
            return registrados;

        var busqueda = await _clienteTesoro.ObtenerBusquedaParticipanteAsync(
            ejecucion.ModoDeJuegoId, cancelacion);
        if (busqueda is null) return registrados;

        var tiempoLimiteMs = (int)Math.Min(
            (long)ejecucion.DuracionSegundos * 1000L, int.MaxValue);
        var tiempoTranscurridoMs = (int)Math.Clamp(
            ejecucion.CalcularTiempoActivoTranscurridoMs(ahoraUtc), 0L, tiempoLimiteMs);

        if (sesion is SesionIndividual individual)
        {
            var totalCompetidores = individual.Participantes.Count;
            foreach (var participante in individual.Participantes)
            {
                var yaTieneEvidencia = await _evidenciasTesoro.ExisteEvidenciaIndividualAsync(
                    sesion.Id, ejecucion.EtapaId, participante.ParticipanteIdentidadId, cancelacion);
                if (yaTieneEvidencia) continue;

                await RegistrarEvidenciaTesoroSinEnviarAsync(
                    sesion.Id,
                    ejecucion,
                    participante,
                    equipoId: null,
                    busqueda.Puntaje,
                    totalCompetidores,
                    tiempoTranscurridoMs,
                    tiempoLimiteMs,
                    ahoraUtc,
                    cancelacion);
                registrados.Add(new ProgresoRegistrado(participante.ParticipanteIdentidadId, null));
            }

            return registrados;
        }

        if (sesion is SesionGrupal grupal)
        {
            var totalCompetidores = grupal.Equipos.Count;
            foreach (var equipo in grupal.Equipos)
            {
                var representante = equipo.Participantes
                    .OrderBy(p => p.FechaUnionEquipo ?? p.FechaUnionSesion)
                    .FirstOrDefault();
                if (representante is null) continue;

                var yaTieneEvidencia = await _evidenciasTesoro.ExisteEvidenciaEquipoAsync(
                    sesion.Id, ejecucion.EtapaId, equipo.Id, cancelacion);
                if (yaTieneEvidencia) continue;

                await RegistrarEvidenciaTesoroSinEnviarAsync(
                    sesion.Id,
                    ejecucion,
                    representante,
                    equipo.Id,
                    busqueda.Puntaje,
                    totalCompetidores,
                    tiempoTranscurridoMs,
                    tiempoLimiteMs,
                    ahoraUtc,
                    cancelacion);
                registrados.Add(new ProgresoRegistrado(representante.ParticipanteIdentidadId, equipo.Id));
            }
        }

        return registrados;
    }

    private async Task RegistrarEvidenciaTesoroSinEnviarAsync(
        Guid sesionId,
        EjecucionActualSesion ejecucion,
        Participante participante,
        Guid? equipoId,
        int puntajeBase,
        int totalCompetidores,
        int tiempoTranscurridoMs,
        int tiempoLimiteMs,
        DateTime ahoraUtc,
        CancellationToken cancelacion)
    {
        var eventoId = Guid.NewGuid();
        await _evidenciasTesoro.AgregarAsync(new EvidenciaTesoroRegistro(
            SesionId: sesionId,
            MisionId: ejecucion.MisionId,
            EtapaId: ejecucion.EtapaId,
            BusquedaId: ejecucion.ModoDeJuegoId,
            ParticipanteIdentidadId: participante.ParticipanteIdentidadId,
            EquipoId: equipoId,
            CodigoEnviado: "__SIN_EVIDENCIA__",
            EsValida: false,
            PuntosGanados: 0,
            EventoPuntuacionId: eventoId,
            FechaEnvioUtc: ahoraUtc),
            cancelacion);

        await _publicadorRanking.PublicarEvidenciaTesoroRegistradaAsync(
            eventoId,
            sesionId,
            ejecucion.MisionId,
            ejecucion.EtapaId,
            participante.Id,
            participante.ParticipanteIdentidadId,
            equipoId,
            ejecucion.ModoDeJuegoId,
            false,
            puntajeBase,
            0,
            totalCompetidores,
            tiempoTranscurridoMs,
            tiempoLimiteMs,
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

    private static bool SesionVencioPorDuracion(Sesion sesion, DateTime ahoraUtc)
    {
        if (!sesion.FechaInicioUtc.HasValue ||
            !sesion.DuracionSegundosLimite.HasValue ||
            sesion.DuracionSegundosLimite.Value <= 0)
            return false;

        return ahoraUtc >= sesion.FechaInicioUtc.Value.AddSeconds(sesion.DuracionSegundosLimite.Value);
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

    private sealed record ProgresoRegistrado(Guid ParticipanteIdentidadId, Guid? EquipoId);
}
