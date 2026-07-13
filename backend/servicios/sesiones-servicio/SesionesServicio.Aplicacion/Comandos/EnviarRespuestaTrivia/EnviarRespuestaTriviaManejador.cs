using MediatR;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Estrategias;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;

public sealed class EnviarRespuestaTriviaManejador
    : IRequestHandler<EnviarRespuestaTriviaComando, EnviarRespuestaTriviaRespuesta>
{
    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IClienteJuegosTrivia _clienteTrivia;
    private readonly IRepositorioRespuestasTrivia _repositorioRespuestas;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IServicioFinalizacionSesion _servicioFinalizacion;
    private readonly IEstrategiaCalculoPuntajeTrivia _estrategiaPuntaje;
    private readonly IServicioProgresoSecuencialSesion _servicioProgresoSecuencial;
    private readonly IServicioTiempoTriviaSesion _servicioTiempoTrivia;
    private readonly IProveedorFechaHora _reloj;
    private readonly IPublicadorEventosRanking _publicadorRanking;

    public EnviarRespuestaTriviaManejador(
        IUsuarioActual usuario,
        IRepositorioSesiones repositorioSesiones,
        IClienteJuegosTrivia clienteTrivia,
        IRepositorioRespuestasTrivia repositorioRespuestas,
        INotificadorSesionesTiempoReal notificador,
        IServicioFinalizacionSesion servicioFinalizacion,
        IEstrategiaCalculoPuntajeTrivia estrategiaPuntaje,
        IServicioProgresoSecuencialSesion servicioProgresoSecuencial,
        IServicioTiempoTriviaSesion servicioTiempoTrivia,
        IProveedorFechaHora reloj,
        IPublicadorEventosRanking publicadorRanking)
    {
        _usuario = usuario;
        _repositorioSesiones = repositorioSesiones;
        _clienteTrivia = clienteTrivia;
        _repositorioRespuestas = repositorioRespuestas;
        _notificador = notificador;
        _servicioFinalizacion = servicioFinalizacion;
        _estrategiaPuntaje = estrategiaPuntaje;
        _servicioProgresoSecuencial = servicioProgresoSecuencial;
        _servicioTiempoTrivia = servicioTiempoTrivia;
        _reloj = reloj;
        _publicadorRanking = publicadorRanking;
    }

    public async Task<EnviarRespuestaTriviaRespuesta> Handle(
        EnviarRespuestaTriviaComando comando, CancellationToken cancelacion)
    {
        var participanteIdentidadId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new OperacionSesionInvalidaExcepcion("La sesion no esta activa.");

        if (sesion.Misiones.All(m => m.MisionId != comando.MisionId))
            throw new MisionNoEncontradaExcepcion("La mision no pertenece a esta sesion.");

        var (participante, totalJugadoresEsperados) =
            ObtenerParticipanteYTotal(sesion, participanteIdentidadId);
        var equipoId = participante.EquipoId;

        await _servicioProgresoSecuencial.ValidarEtapaActualAsync(
            sesion,
            participanteIdentidadId,
            comando.MisionId,
            comando.EtapaId,
            "Trivia",
            comando.TriviaId,
            cancelacion);

        var trivia = await _clienteTrivia.ObtenerTriviaParticipanteAsync(comando.TriviaId, cancelacion)
            ?? throw new InvalidOperationException("Trivia no encontrada.");
        var pregunta = trivia.Preguntas.FirstOrDefault(p => p.Id == comando.PreguntaId)
            ?? throw new InvalidOperationException("Pregunta no encontrada.");
        var ejecucion = sesion.EjecucionActual
            ?? throw new OperacionSesionInvalidaExcepcion("La sesion no tiene una etapa activa.");
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var respuestasPrevias = await _repositorioRespuestas.ObtenerRespuestasConTiempoAsync(
            comando.SesionId, comando.EtapaId, participanteIdentidadId, equipoId, cancelacion);
        var tiempoTrivia = _servicioTiempoTrivia.Calcular(ejecucion, trivia, respuestasPrevias, ahoraUtc);
        var ventanaPregunta = tiempoTrivia.ObtenerPregunta(comando.PreguntaId)
            ?? throw new InvalidOperationException("Pregunta no encontrada en la trivia.");

        var yaRespondioAntes = await _repositorioRespuestas.ExisteRespuestaOficialAsync(
            comando.SesionId, comando.EtapaId, comando.PreguntaId,
            participanteIdentidadId, equipoId, cancelacion);

        if (yaRespondioAntes)
            throw new RespuestaTriviaDuplicadaExcepcion(esEquipo: equipoId.HasValue);

        await RegistrarTimeoutsExpiradosAsync(
            comando,
            tiempoTrivia,
            participanteIdentidadId,
            equipoId,
            ahoraUtc,
            cancelacion);

        var esCorrecta = false;
        var puntosGanados = 0;
        var tiempoTardadoMs = CalcularTiempoTardadoServidor(ventanaPregunta, tiempoTrivia.TiempoActivoEtapaMs);
        var respuestaRegistradaPorTimeout = ventanaPregunta.Expirada;

        if (!respuestaRegistradaPorTimeout && !ventanaPregunta.Actual)
        {
            throw new OperacionSesionInvalidaExcepcion(
                "La pregunta no esta dentro de su ventana temporal activa.");
        }

        if (!respuestaRegistradaPorTimeout && !comando.OpcionSeleccionadaId.HasValue)
        {
            throw new OperacionSesionInvalidaExcepcion(
                "La pregunta aun no ha agotado su ventana temporal.");
        }

        if (!respuestaRegistradaPorTimeout && comando.OpcionSeleccionadaId.HasValue)
        {
            var verificacion = await _clienteTrivia.VerificarRespuestaAsync(
                comando.TriviaId,
                comando.PreguntaId,
                comando.OpcionSeleccionadaId.Value,
                cancelacion)
                ?? throw new InvalidOperationException("Pregunta u opcion no encontrada.");

            esCorrecta = verificacion.EsCorrecta;
            puntosGanados = _estrategiaPuntaje.Calcular(new ContextoCalculoPuntajeTrivia(
                EsCorrecta: verificacion.EsCorrecta,
                PuntajeBase: verificacion.PuntajeBase,
                TiempoTardadoMs: tiempoTardadoMs,
                TiempoLimiteMs: ventanaPregunta.DuracionMs));
        }

        if (!respuestaRegistradaPorTimeout)
        {
            await _repositorioRespuestas.AgregarAsync(new RespuestaTriviaRegistro(
                SesionId: comando.SesionId,
                MisionId: comando.MisionId,
                EtapaId: comando.EtapaId,
                TriviaId: comando.TriviaId,
                PreguntaId: comando.PreguntaId,
                OpcionSeleccionadaId: comando.OpcionSeleccionadaId,
                ParticipanteIdentidadId: participanteIdentidadId,
                EquipoId: equipoId,
                EsCorrecta: esCorrecta,
                PuntosGanados: puntosGanados,
                TiempoTardadoMs: tiempoTardadoMs,
                FechaRespuestaUtc: ahoraUtc),
                cancelacion);

            await _notificador.NotificarRespuestaRegistradaAsync(
                comando.SesionId,
                comando.EtapaId,
                comando.PreguntaId,
                participanteIdentidadId,
                equipoId,
                esCorrecta,
                puntosGanados,
                cancelacion);

            await _publicadorRanking.PublicarRespuestaTriviaRegistradaAsync(
                comando.SesionId, participante.Id, participanteIdentidadId,
                equipoId, puntosGanados, cancelacion);
        }

        var totalPreguntas = trivia.Preguntas.Count;
        var etapaCompletada = false;
        var preguntasDistintas = await _repositorioRespuestas.ContarPreguntasDistintasDeJugadorEnEtapaAsync(
            comando.SesionId, comando.EtapaId, participanteIdentidadId, equipoId, cancelacion);

        if (totalPreguntas > 0 && preguntasDistintas >= totalPreguntas)
        {
            await _notificador.NotificarProgresoSecuencialActualizadoAsync(
                comando.SesionId, participanteIdentidadId, equipoId, cancelacion);

            var jugadoresCompletaron = await _repositorioRespuestas.ContarJugadoresQueCompletaronEtapaAsync(
                comando.SesionId, comando.EtapaId, totalPreguntas, cancelacion);

            if (jugadoresCompletaron >= totalJugadoresEsperados)
            {
                etapaCompletada = true;
                // NO se cierra inmediatamente: se entra en CierrePendiente para que
                // el último jugador vea su feedback final antes de arrancar la
                // preparación de 10 s. El worker cierra la etapa al vencer (#10/#13).
                await _servicioFinalizacion.ProgramarCierreTrasFeedbackAsync(
                    comando.SesionId, comando.EtapaId, cancelacion);
            }
        }

        return new EnviarRespuestaTriviaRespuesta(
            esCorrecta, puntosGanados, etapaCompletada);
    }

    private async Task RegistrarTimeoutsExpiradosAsync(
        EnviarRespuestaTriviaComando comando,
        EstadoTiempoTriviaSesion tiempoTrivia,
        Guid participanteIdentidadId,
        Guid? equipoId,
        DateTime ahoraUtc,
        CancellationToken cancelacion)
    {
        foreach (var preguntaExpiradaId in tiempoTrivia.PreguntasExpiradasIds)
        {
            var yaRespondida = await _repositorioRespuestas.ExisteRespuestaOficialAsync(
                comando.SesionId,
                comando.EtapaId,
                preguntaExpiradaId,
                participanteIdentidadId,
                equipoId,
                cancelacion);

            if (yaRespondida) continue;

            var ventana = tiempoTrivia.ObtenerPregunta(preguntaExpiradaId);
            if (ventana is null) continue;

            try
            {
                await _repositorioRespuestas.AgregarAsync(new RespuestaTriviaRegistro(
                    SesionId: comando.SesionId,
                    MisionId: comando.MisionId,
                    EtapaId: comando.EtapaId,
                    TriviaId: comando.TriviaId,
                    PreguntaId: preguntaExpiradaId,
                    OpcionSeleccionadaId: null,
                    ParticipanteIdentidadId: participanteIdentidadId,
                    EquipoId: equipoId,
                    EsCorrecta: false,
                    PuntosGanados: 0,
                    TiempoTardadoMs: ventana.DuracionMs,
                    FechaRespuestaUtc: ahoraUtc),
                    cancelacion);
            }
            catch (RespuestaTriviaDuplicadaExcepcion)
            {
                // La restriccion unica define la respuesta oficial si otra peticion gano la carrera.
            }
        }
    }

    private static int CalcularTiempoTardadoServidor(
        VentanaPreguntaTriviaSesion ventana,
        long tiempoActivoEtapaMs)
        => (int)Math.Clamp(tiempoActivoEtapaMs - ventana.InicioMs, 0, ventana.DuracionMs);

    private static (Participante participante, int totalJugadores) ObtenerParticipanteYTotal(
        Sesion sesion, Guid participanteIdentidadId)
    {
        if (sesion is SesionIndividual individual)
        {
            var p = individual.Participantes
                .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteIdentidadId)
                ?? throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return (p, individual.Participantes.Count);
        }

        if (sesion is SesionGrupal grupal)
        {
            foreach (var equipo in grupal.Equipos)
            {
                var p = equipo.Participantes
                    .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteIdentidadId);
                if (p is not null)
                    return (p, grupal.Equipos.Count);
            }

            throw new ParticipacionInvalidaExcepcion("El participante no esta inscrito en esta sesion.");
        }

        throw new SesionInvalidaExcepcion("Tipo de sesion no soportado.");
    }
}
