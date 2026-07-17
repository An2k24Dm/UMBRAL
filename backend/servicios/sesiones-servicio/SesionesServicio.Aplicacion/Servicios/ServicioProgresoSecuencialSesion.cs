using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Servicios;

public sealed class ServicioProgresoSecuencialSesion : IServicioProgresoSecuencialSesion
{
    private const string TipoTrivia = "Trivia";
    private const string TipoTesoro = "BusquedaTesoro";
    private const string FaseActiva = "Activa";

    private readonly IUsuarioActual _usuarioActual;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IClienteJuegosTrivia _clienteTrivia;
    private readonly IRepositorioRespuestasTrivia _repositorioRespuestasTrivia;
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidenciasTesoro;
    private readonly IRepositorioEtapasCompletadas _repositorioEtapasCompletadas;
    private readonly IProveedorFechaHora _reloj;
    private readonly IServicioTiempoTriviaSesion _servicioTiempoTrivia;
    private readonly IServicioFinalizacionSesion _finalizacion;

    public ServicioProgresoSecuencialSesion(
        IUsuarioActual usuarioActual,
        IRepositorioSesiones repositorioSesiones,
        IClienteJuegosTrivia clienteTrivia,
        IRepositorioRespuestasTrivia repositorioRespuestasTrivia,
        IRepositorioEvidenciasTesoro repositorioEvidenciasTesoro,
        IRepositorioEtapasCompletadas repositorioEtapasCompletadas,
        IProveedorFechaHora reloj,
        IServicioTiempoTriviaSesion servicioTiempoTrivia,
        IServicioFinalizacionSesion finalizacion)
    {
        _usuarioActual = usuarioActual;
        _repositorioSesiones = repositorioSesiones;
        _clienteTrivia = clienteTrivia;
        _repositorioRespuestasTrivia = repositorioRespuestasTrivia;
        _repositorioEvidenciasTesoro = repositorioEvidenciasTesoro;
        _repositorioEtapasCompletadas = repositorioEtapasCompletadas;
        _reloj = reloj;
        _servicioTiempoTrivia = servicioTiempoTrivia;
        _finalizacion = finalizacion;
    }

    public async Task<ProgresoSecuencialSesionDto> ObtenerParaParticipanteActualAsync(
        Guid sesionId,
        CancellationToken cancelacion)
    {
        var participanteIdentidadId = _usuarioActual.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");
        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(sesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");
        if (await _finalizacion.FinalizarSesionSiDuracionVencidaAsync(
                sesion.Id, cancelacion))
        {
            sesion = await _repositorioSesiones.ObtenerPorIdAsync(sesionId, cancelacion)
                ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");
        }

        return await ConstruirAsync(sesion, participanteIdentidadId, cancelacion);
    }

    public async Task ValidarEtapaActualAsync(
        Sesion sesion,
        Guid participanteIdentidadId,
        Guid misionId,
        Guid etapaId,
        string tipoEtapa,
        Guid modoDeJuegoId,
        CancellationToken cancelacion)
    {
        var progreso = await ConstruirAsync(sesion, participanteIdentidadId, cancelacion);

        if (progreso.TodoCompletado ||
            progreso.MisionActualId != misionId ||
            progreso.EtapaActualId != etapaId ||
            !string.Equals(progreso.TipoEtapaActual, tipoEtapa, StringComparison.OrdinalIgnoreCase) ||
            progreso.ModoDeJuegoId != modoDeJuegoId ||

            !string.Equals(progreso.FaseEtapaActual, FaseActiva, StringComparison.OrdinalIgnoreCase) ||
            progreso.JugadorActualCompletoEtapaActual)
        {
            throw new OperacionSesionInvalidaExcepcion(
                "Debes jugar la etapa global activa de la sesion.");
        }
    }

    private async Task<ProgresoSecuencialSesionDto> ConstruirAsync(
        Sesion sesion,
        Guid participanteIdentidadId,
        CancellationToken cancelacion)
    {
        var equipoId = ObtenerEquipoId(sesion, participanteIdentidadId);
        var completadasGlobalmente = await _repositorioEtapasCompletadas
            .ObtenerCompletadasAsync(sesion.Id, cancelacion);

        if (sesion.EjecucionActual is null)
        {
            return new ProgresoSecuencialSesionDto
            {
                EtapasCompletadasGlobalmenteIds = completadasGlobalmente.ToList(),
                TodoCompletado = true
            };
        }

        var actual = sesion.EjecucionActual;
        var jugadorCompleto = await EstaCompletadaAsync(
            sesion.Id,
            new EtapaSecuencial(
                actual.MisionId,
                actual.EtapaId,
                actual.TipoEtapa,
                actual.ModoDeJuegoId,
                actual.OrdenGlobal),
            participanteIdentidadId,
            equipoId,
            cancelacion);

        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var enPreparacion = actual.EstaEnPreparacion;
        var esActiva = actual.EstaActiva;
        var segundosRestantesPreparacion = enPreparacion
            ? actual.CalcularSegundosRestantesPreparacion(ahoraUtc)
            : 0;

        var dto = new ProgresoSecuencialSesionDto
        {
            EtapasCompletadasGlobalmenteIds = completadasGlobalmente.ToList(),
            MisionActualId = actual.MisionId,
            EtapaActualId = actual.EtapaId,
            TipoEtapaActual = actual.TipoEtapa,
            ModoDeJuegoId = actual.ModoDeJuegoId,
            OrdenGlobalActual = actual.OrdenGlobal,
            FaseEtapaActual = actual.Fase.ToString(),
            NumeroMisionActual = actual.OrdenMision,
            NumeroEtapaActual = actual.OrdenEtapa,
            EsNuevaMision = actual.EsNuevaMision,
            FechaInicioEtapaUtc = actual.FechaInicioUtc,
            DuracionEtapaSegundos = actual.DuracionSegundos,
            DuracionPausasAcumuladaMs = actual.DuracionPausasAcumuladaMs,
            FechaInicioPausaUtc = actual.FechaInicioPausaUtc,

            SegundosRestantesEtapa = esActiva
                ? actual.CalcularSegundosRestantes(ahoraUtc)
                : actual.DuracionSegundos,
            TiempoActivoEtapaMs = esActiva
                ? actual.CalcularTiempoActivoTranscurridoMs(ahoraUtc)
                : 0,

            SegundosRestantesPreparacion = enPreparacion ? segundosRestantesPreparacion : null,
            DuracionPreparacionSegundos = enPreparacion ? actual.DuracionPreparacionSegundos : null,
            FechaInicioProgramadaEtapaUtc = enPreparacion
                ? ahoraUtc.AddSeconds(segundosRestantesPreparacion)
                : null,
            JugadorActualCompletoEtapaActual = jugadorCompleto,
            EsperandoOtrosJugadores = jugadorCompleto,
            TodoCompletado = false
        };

        if (esActiva &&
            string.Equals(actual.TipoEtapa, TipoTrivia, StringComparison.OrdinalIgnoreCase))
        {
            var trivia = await _clienteTrivia.ObtenerTriviaParticipanteAsync(
                actual.ModoDeJuegoId,
                cancelacion);
            if (trivia is not null)
            {
                var respuestas = await _repositorioRespuestasTrivia.ObtenerRespuestasConTiempoAsync(
                    sesion.Id, actual.EtapaId, participanteIdentidadId, equipoId, cancelacion);
                var tiempoTrivia = _servicioTiempoTrivia.Calcular(actual, trivia, respuestas, ahoraUtc);
                dto.TriviaPreguntaActualId = tiempoTrivia.PreguntaActualId;
                dto.TriviaPreguntasExpiradasIds = tiempoTrivia.PreguntasExpiradasIds.ToList();
                dto.TriviaTiempoRestantePreguntaMs = tiempoTrivia.TiempoRestantePreguntaMs;
                dto.TriviaTiempoTranscurridoPreguntaMs = tiempoTrivia.TiempoTranscurridoPreguntaMs;
                dto.TriviaAgotada = tiempoTrivia.TriviaAgotada;
                dto.TriviaEnTransicionEntrePreguntas = tiempoTrivia.EnTransicionEntrePreguntas;
                dto.TriviaTiempoRestanteTransicionMs = tiempoTrivia.TiempoRestanteTransicionMs;
                dto.TriviaSiguientePreguntaId = tiempoTrivia.SiguientePreguntaId;
            }
        }

        return dto;
    }

    private async Task<bool> EstaCompletadaAsync(
        Guid sesionId,
        EtapaSecuencial etapa,
        Guid participanteIdentidadId,
        Guid? equipoId,
        CancellationToken cancelacion)
    {
        if (string.Equals(etapa.TipoEtapa, TipoTrivia, StringComparison.OrdinalIgnoreCase))
        {
            var trivia = await _clienteTrivia.ObtenerTriviaParticipanteAsync(
                etapa.ModoDeJuegoId,
                cancelacion);
            var totalPreguntas = trivia?.Preguntas.Count ?? 0;
            if (totalPreguntas <= 0) return false;

            var respondidas = await _repositorioRespuestasTrivia
                .ContarPreguntasDistintasDeJugadorEnEtapaAsync(
                    sesionId,
                    etapa.EtapaId,
                    participanteIdentidadId,
                    equipoId,
                    cancelacion);

            return respondidas >= totalPreguntas;
        }

        if (string.Equals(etapa.TipoEtapa, TipoTesoro, StringComparison.OrdinalIgnoreCase))
        {
            return equipoId.HasValue
                ? await _repositorioEvidenciasTesoro.ExisteEvidenciaValidaEquipoAsync(
                    sesionId, etapa.EtapaId, equipoId.Value, cancelacion)
                : await _repositorioEvidenciasTesoro.ExisteEvidenciaValidaIndividualAsync(
                    sesionId, etapa.EtapaId, participanteIdentidadId, cancelacion);
        }

        return false;
    }

    private static Guid? ObtenerEquipoId(Sesion sesion, Guid participanteIdentidadId)
    {
        if (sesion is SesionIndividual individual)
        {
            if (!individual.Participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId))
                throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return null;
        }

        if (sesion is SesionGrupal grupal)
        {
            var equipo = grupal.Equipos.FirstOrDefault(e =>
                e.Participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId));
            if (equipo is null)
                throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return equipo.Id;
        }

        throw new SesionInvalidaExcepcion("Tipo de sesion no soportado.");
    }

    private sealed record EtapaSecuencial(
        Guid MisionId,
        Guid EtapaId,
        string TipoEtapa,
        Guid ModoDeJuegoId,
        int OrdenGlobal);
}
