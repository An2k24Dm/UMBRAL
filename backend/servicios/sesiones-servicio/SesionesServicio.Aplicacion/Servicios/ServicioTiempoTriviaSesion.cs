using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Servicios;

public sealed class ServicioTiempoTriviaSesion : IServicioTiempoTriviaSesion
{
    public const int DuracionFeedbackPreguntaSegundosPorDefecto = 5;

    private readonly int _duracionFeedbackMs;

    public ServicioTiempoTriviaSesion(
        int duracionFeedbackPreguntaSegundos = DuracionFeedbackPreguntaSegundosPorDefecto)
    {
        _duracionFeedbackMs = Math.Max(
            DuracionFeedbackPreguntaSegundosPorDefecto,
            duracionFeedbackPreguntaSegundos) * 1000;
    }

    public EstadoTiempoTriviaSesion Calcular(
        EjecucionActualSesion ejecucion,
        TriviaParticipanteJuegosDto trivia,
        IReadOnlyCollection<RespuestaTriviaTiempo> respuestasPrevias,
        DateTime ahoraUtc)
    {
        var tiempoActivoMs = ejecucion.CalcularTiempoActivoTranscurridoMs(ahoraUtc);

        var restanteGlobalMs = Math.Max(
            0L, (long)ejecucion.DuracionSegundos * 1000 - tiempoActivoMs);

        var tardadoPorPregunta = new Dictionary<Guid, int>();
        foreach (var respuesta in respuestasPrevias)
            tardadoPorPregunta[respuesta.PreguntaId] = respuesta.TiempoTardadoMs;

        var ventanas = new List<VentanaPreguntaTriviaSesion>();
        long cursorMs = 0;
        Guid? preguntaActualId = null;
        int? tiempoTranscurridoPreguntaMs = null;
        int? tiempoRestantePreguntaMs = null;
        var enTransicion = false;
        int? tiempoRestanteTransicionMs = null;
        Guid? siguientePreguntaId = null;

        foreach (var pregunta in trivia.Preguntas)
        {
            var duracionSegundos = pregunta.TiempoEstimado > 0
                ? pregunta.TiempoEstimado
                : trivia.TiempoLimitePorPregunta;
            var duracionMs = Math.Max(1, duracionSegundos) * 1000;
            var inicioMs = cursorMs;
            var finMs = cursorMs + duracionMs;

            if (tardadoPorPregunta.TryGetValue(pregunta.Id, out var tardado))
            {
                var consumidoMs = (int)Math.Clamp(tardado, 0, duracionMs);
                ventanas.Add(new VentanaPreguntaTriviaSesion(
                    pregunta.Id, inicioMs, finMs, duracionMs, Expirada: false, Actual: false));
                cursorMs += consumidoMs + _duracionFeedbackMs;
                continue;
            }

            if (preguntaActualId is not null || enTransicion)
            {
                ventanas.Add(new VentanaPreguntaTriviaSesion(
                    pregunta.Id, inicioMs, finMs, duracionMs, Expirada: false, Actual: false));
                cursorMs += duracionMs + _duracionFeedbackMs;
                continue;
            }

            var transcurridoMs = tiempoActivoMs - inicioMs;

            if (transcurridoMs < 0)
            {
                enTransicion = true;
                siguientePreguntaId = pregunta.Id;
                tiempoRestanteTransicionMs = (int)Math.Max(0, inicioMs - tiempoActivoMs);
                ventanas.Add(new VentanaPreguntaTriviaSesion(
                    pregunta.Id, inicioMs, finMs, duracionMs, Expirada: false, Actual: false));
                cursorMs += duracionMs + _duracionFeedbackMs;
                continue;
            }

            if (transcurridoMs >= duracionMs)
            {
                ventanas.Add(new VentanaPreguntaTriviaSesion(
                    pregunta.Id, inicioMs, finMs, duracionMs, Expirada: true, Actual: false));
                cursorMs += duracionMs + _duracionFeedbackMs;
                continue;
            }

            preguntaActualId = pregunta.Id;
            var transcurridoActual = (int)Math.Clamp(transcurridoMs, 0, duracionMs);
            tiempoTranscurridoPreguntaMs = transcurridoActual;
            tiempoRestantePreguntaMs = (int)Math.Max(
                0, Math.Min((long)(duracionMs - transcurridoActual), restanteGlobalMs));
            ventanas.Add(new VentanaPreguntaTriviaSesion(
                pregunta.Id, inicioMs, finMs, duracionMs, Expirada: false, Actual: true));
            cursorMs += duracionMs + _duracionFeedbackMs;
        }

        var triviaAgotada = ventanas.Count > 0 && preguntaActualId is null && !enTransicion;

        return new EstadoTiempoTriviaSesion(
            tiempoActivoMs,
            ventanas,
            preguntaActualId,
            tiempoRestantePreguntaMs,
            tiempoTranscurridoPreguntaMs,
            triviaAgotada,
            enTransicion,
            tiempoRestanteTransicionMs,
            siguientePreguntaId);
    }
}
