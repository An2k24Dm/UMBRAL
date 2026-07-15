using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Servicios;

public class ServicioTiempoTriviaSesionPruebas
{
    private static readonly DateTime InicioUtc = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid MisionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EtapaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TriviaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Pregunta1 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Pregunta2 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Pregunta3 = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // Feedback autoritativo de 5 s entre preguntas.
    private readonly ServicioTiempoTriviaSesion _servicio = new(5);

    // -- Sin respuestas: la pregunta 1 corre desde el inicio de la etapa. --

    [Fact]
    public void EntradaEnT4_SinRespuestas_DevuelvePregunta1ConDieciseisSegundosRestantes()
    {
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(), SinRespuestas(), InicioUtc.AddSeconds(4));

        estado.PreguntaActualId.Should().Be(Pregunta1);
        estado.TiempoTranscurridoPreguntaMs.Should().Be(4_000);
        estado.TiempoRestantePreguntaMs.Should().Be(16_000);
        estado.PreguntasExpiradasIds.Should().BeEmpty();
        estado.EnTransicionEntrePreguntas.Should().BeFalse();
        estado.TriviaAgotada.Should().BeFalse();
    }

    [Fact]
    public void PreguntaSinResponderVence_TrasElFeedbackLaSiguienteSeVuelveActual()
    {
        // P1 [0,20). Vence en t=20 → feedback [20,25) → P2 [25,40).
        // En t=28: P2 activa con transcurrido 3 s (restante 12 s); P1 expirada.
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(), SinRespuestas(), InicioUtc.AddSeconds(28));

        estado.PreguntaActualId.Should().Be(Pregunta2);
        estado.TiempoTranscurridoPreguntaMs.Should().Be(3_000);
        estado.TiempoRestantePreguntaMs.Should().Be(12_000);
        estado.PreguntasExpiradasIds.Should().ContainSingle().Which.Should().Be(Pregunta1);
    }

    [Fact]
    public void PausaNoConsumeVentanaTemporalDePregunta()
    {
        var ejecucion = CrearEjecucion()
            .Pausar(InicioUtc.AddSeconds(7))
            .Reanudar(InicioUtc.AddSeconds(37));

        var estado = _servicio.Calcular(
            ejecucion, CrearTrivia(), SinRespuestas(), InicioUtc.AddSeconds(37));

        estado.PreguntaActualId.Should().Be(Pregunta1);
        estado.TiempoTranscurridoPreguntaMs.Should().Be(7_000);
        estado.TiempoRestantePreguntaMs.Should().Be(13_000);
    }

    // -- Feedback autoritativo de 5 s entre preguntas (#2/#3). --

    [Fact]
    public void RespondeP1EnT5_HayFeedbackDe5s_LaP2NoConsumeTiempo()
    {
        // Respondió P1 en 5 s. En t=5 empieza el feedback; P2 no es respondible aún.
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(),
            new[] { new RespuestaTriviaTiempo(Pregunta1, 5_000) },
            InicioUtc.AddSeconds(5));

        estado.PreguntaActualId.Should().BeNull();
        estado.EnTransicionEntrePreguntas.Should().BeTrue();
        estado.SiguientePreguntaId.Should().Be(Pregunta2);
        estado.TiempoRestanteTransicionMs.Should().Be(5_000);
        estado.TriviaAgotada.Should().BeFalse();
    }

    [Fact]
    public void ReconexionA3sDelFeedback_QuedanDosSegundos_NoSeReinicia()
    {
        // #5/#27-caso2: el feedback empezó en t=5; consultar en t=8 deja ~2 s.
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(),
            new[] { new RespuestaTriviaTiempo(Pregunta1, 5_000) },
            InicioUtc.AddSeconds(8));

        estado.EnTransicionEntrePreguntas.Should().BeTrue();
        estado.SiguientePreguntaId.Should().Be(Pregunta2);
        estado.TiempoRestanteTransicionMs.Should().Be(2_000);
        estado.PreguntaActualId.Should().BeNull();
    }

    [Fact]
    public void TrasElFeedback_LaP2SeVuelveActualConSuVentanaCompleta()
    {
        // P1 respondida en 5 s → feedback [5,10) → P2 [10,25). En t=10, P2 activa.
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(),
            new[] { new RespuestaTriviaTiempo(Pregunta1, 5_000) },
            InicioUtc.AddSeconds(10));

        estado.PreguntaActualId.Should().Be(Pregunta2);
        estado.EnTransicionEntrePreguntas.Should().BeFalse();
        estado.TiempoTranscurridoPreguntaMs.Should().Be(0);
        estado.TiempoRestantePreguntaMs.Should().Be(15_000);
    }

    [Fact]
    public void RespondeP1YP2_TrasSusFeedbacks_LaP3SeVuelveActual()
    {
        // P1 5 s → feedback → P2 abre en t=10, respondida en 4 s (t=14) → feedback
        // [14,19) → P3 [19,29). En t=19, P3 activa con sus 10 s.
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(),
            new[]
            {
                new RespuestaTriviaTiempo(Pregunta1, 5_000),
                new RespuestaTriviaTiempo(Pregunta2, 4_000)
            },
            InicioUtc.AddSeconds(19));

        estado.PreguntaActualId.Should().Be(Pregunta3);
        estado.TiempoTranscurridoPreguntaMs.Should().Be(0);
        estado.TiempoRestantePreguntaMs.Should().Be(10_000);
    }

    [Fact]
    public void TodasRespondidas_TriviaAgotadaSinPreguntaActualNiTransicion()
    {
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(),
            new[]
            {
                new RespuestaTriviaTiempo(Pregunta1, 5_000),
                new RespuestaTriviaTiempo(Pregunta2, 4_000),
                new RespuestaTriviaTiempo(Pregunta3, 3_000)
            },
            InicioUtc.AddSeconds(40));

        estado.PreguntaActualId.Should().BeNull();
        estado.EnTransicionEntrePreguntas.Should().BeFalse();
        estado.TriviaAgotada.Should().BeTrue();
        estado.TiempoRestantePreguntaMs.Should().BeNull();
    }

    [Fact]
    public void TiempoTardadoMayorAlLimite_SeRecortaAlLimiteAlEncadenar()
    {
        // Aunque una respuesta traiga un tardado enorme, no desplaza la siguiente
        // más allá de la ventana de la anterior + feedback: P2 abre en t=25.
        var estado = _servicio.Calcular(
            CrearEjecucion(), CrearTrivia(),
            new[] { new RespuestaTriviaTiempo(Pregunta1, 99_999) },
            InicioUtc.AddSeconds(25));

        estado.PreguntaActualId.Should().Be(Pregunta2);
        estado.TiempoTranscurridoPreguntaMs.Should().Be(0);
        estado.TiempoRestantePreguntaMs.Should().Be(15_000);
    }

    [Fact]
    public void RestantePreguntaNuncaSuperaElRestanteGlobalDeLaEtapa()
    {
        var ejecucionCorta = EjecucionActualSesion.Crear(
            MisionId, EtapaId, TriviaId, "Trivia", 1, InicioUtc, 8);

        var estado = _servicio.Calcular(
            ejecucionCorta, CrearTrivia(), SinRespuestas(), InicioUtc.AddSeconds(3));

        estado.PreguntaActualId.Should().Be(Pregunta1);
        estado.TiempoRestantePreguntaMs.Should().Be(5_000); // 8 s − 3 s, no 17 s
    }

    private static IReadOnlyCollection<RespuestaTriviaTiempo> SinRespuestas()
        => Array.Empty<RespuestaTriviaTiempo>();

    private static EjecucionActualSesion CrearEjecucion()
        => EjecucionActualSesion.Crear(
            MisionId, EtapaId, TriviaId, "Trivia", 1, InicioUtc, 90);

    private static TriviaParticipanteJuegosDto CrearTrivia()
        => new()
        {
            Id = TriviaId,
            Preguntas =
            {
                new PreguntaParticipanteJuegosDto { Id = Pregunta1, TiempoEstimado = 20 },
                new PreguntaParticipanteJuegosDto { Id = Pregunta2, TiempoEstimado = 15 },
                new PreguntaParticipanteJuegosDto { Id = Pregunta3, TiempoEstimado = 10 }
            }
        };
}
