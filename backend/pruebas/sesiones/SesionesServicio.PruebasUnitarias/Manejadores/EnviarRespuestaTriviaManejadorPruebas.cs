using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Estrategias;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class EnviarRespuestaTriviaManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtroParticipante = Guid.Parse("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TriviaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid PreguntaId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid OpcionId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    // Integrantes de sesión grupal.
    private static readonly Guid Ana = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid Pedro = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
    private static readonly Guid Beto = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3");

    // ----------------------------------------------------------------------
    // Constructores de sesiones
    // ----------------------------------------------------------------------

    private static SesionIndividual IndividualActiva(Guid? participanteExtra = null)
    {
        var s = SesionIndividual.Crear(
            "Trivia", "Demo", AhoraUtc.AddHours(1), "TRIV01", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new[] { MisionId });
        s.Preparar();
        s.AgregarParticipante(ParticipanteId, AhoraUtc);
        if (participanteExtra.HasValue) s.AgregarParticipante(participanteExtra.Value, AhoraUtc);
        s.IniciarPrimeraEtapa(MisionId, EtapaId, TriviaId, "Trivia", 1, AhoraUtc, 60);
        return s;
    }

    private static (SesionGrupal sesion, Guid rojoId, Guid azulId) GrupalActiva()
    {
        var s = SesionGrupal.Crear(
            "Trivia", "Demo", AhoraUtc.AddHours(1), "TRIV01", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);
        s.AsignarMisiones(new[] { MisionId });
        s.Preparar();
        var rojo = s.CrearEquipo("Rojo", Ana, AhoraUtc, AhoraUtc);   // Ana líder
        s.AgregarParticipanteAEquipo(rojo.Id, Pedro, AhoraUtc, AhoraUtc);
        var azul = s.CrearEquipo("Azul", Beto, AhoraUtc, AhoraUtc);  // Beto líder
        s.IniciarPrimeraEtapa(MisionId, EtapaId, TriviaId, "Trivia", 1, AhoraUtc, 60);
        return (s, rojo.Id, azul.Id);
    }

    private static VerificacionRespuestaJuegosDto Verif(
        bool correcta = true, int puntaje = 100, int limite = 10)
        => new() { EsCorrecta = correcta, PuntajeBase = puntaje, TiempoLimiteSegundos = limite };

    private static TriviaParticipanteJuegosDto TriviaCon(int totalPreguntas)
    {
        var t = new TriviaParticipanteJuegosDto { Id = TriviaId, TiempoLimitePorPregunta = 10 };
        for (var i = 0; i < totalPreguntas; i++)
            t.Preguntas.Add(new PreguntaParticipanteJuegosDto
            {
                Id = i == 0 ? PreguntaId : Guid.NewGuid()
            });
        return t;
    }

    // ----------------------------------------------------------------------
    // Arranque configurable del manejador con dobles
    // ----------------------------------------------------------------------

    private sealed class Arranque
    {
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IRepositorioSesiones> RepoSesiones { get; } = new();
        public Mock<IClienteJuegosTrivia> ClienteTrivia { get; } = new();
        public Mock<IRepositorioRespuestasTrivia> RepoRespuestas { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IServicioFinalizacionSesion> Finalizacion { get; } = new();
        public Mock<IServicioProgresoSecuencialSesion> ProgresoSecuencial { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public IServicioTiempoTriviaSesion ServicioTiempoTrivia { get; } =
            new ServicioTiempoTriviaSesion();
        public IEstrategiaCalculoPuntajeTrivia Estrategia { get; set; } =
            new EstrategiaPuntajeTriviaPorTiempo();

        public Arranque(
            Sesion sesion,
            Guid pid,
            bool existeRespuesta = false,
            VerificacionRespuestaJuegosDto? verificacion = null,
            int totalPreguntas = 1,
            int preguntasDistintas = 0,
            int jugadoresCompletaron = 0)
        {
            Usuario.Setup(u => u.ObtenerId()).Returns(pid);
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc.AddSeconds(1));
            RepoSesiones.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            RepoRespuestas.Setup(r => r.ExisteRespuestaOficialAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existeRespuesta);

            // Sin respuestas previas por defecto: la pregunta actual arranca en la
            // ventana [0, límite) igual que antes (los tests de tiempo del servidor
            // no cambian). Casos concretos pueden sobrescribir este comportamiento.
            RepoRespuestas.Setup(r => r.ObtenerRespuestasConTiempoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<RespuestaTriviaTiempo>());

            ProgresoSecuencial.Setup(s => s.ValidarEtapaActualAsync(
                    It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            ClienteTrivia.Setup(c => c.VerificarRespuestaAsync(
                    TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verificacion ?? Verif());

            ClienteTrivia.Setup(c => c.ObtenerTriviaParticipanteAsync(
                    TriviaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(TriviaCon(totalPreguntas));

            RepoRespuestas.Setup(r => r.AgregarAsync(
                    It.IsAny<RespuestaTriviaRegistro>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Notificador.Setup(n => n.NotificarRespuestaRegistradaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            RepoRespuestas.Setup(r => r.ContarPreguntasDistintasDeJugadorEnEtapaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(preguntasDistintas);

            RepoRespuestas.Setup(r => r.ContarJugadoresQueCompletaronEtapaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jugadoresCompletaron);

            Notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Notificador.Setup(n => n.NotificarProgresoSecuencialActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Finalizacion.Setup(s => s.ProgramarCierreTrasFeedbackAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public EnviarRespuestaTriviaManejador Construir()
            => new(
                Usuario.Object, RepoSesiones.Object, ClienteTrivia.Object,
                RepoRespuestas.Object, Notificador.Object, Finalizacion.Object, Estrategia,
                ProgresoSecuencial.Object, ServicioTiempoTrivia, Reloj.Object,
                Mock.Of<IPublicadorEventosRanking>());

        public Task<EnviarRespuestaTriviaRespuesta> EjecutarAsync(Guid sesionId, int tiempoMs = 0)
            => Construir().Handle(
                new EnviarRespuestaTriviaComando(
                    sesionId, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, tiempoMs),
                CancellationToken.None);

        public Task<EnviarRespuestaTriviaRespuesta> EjecutarTimeoutAsync(Guid sesionId, int tiempoMs = 11_000)
        {
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc.AddMilliseconds(tiempoMs));
            return Construir().Handle(
                    new EnviarRespuestaTriviaComando(
                        sesionId, MisionId, EtapaId, TriviaId, PreguntaId, null, tiempoMs),
                    CancellationToken.None);
        }
    }

    // ======================================================================
    // SESIÓN INDIVIDUAL
    // ======================================================================

    [Fact] // (1)
    public async Task Individual_ParticipantePuedeResponderUnaVez_PersisteYNotifica()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, verificacion: Verif(puntaje: 50));

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EsCorrecta.Should().BeTrue();
        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x =>
                x.SesionId == sesion.Id &&
                x.PreguntaId == PreguntaId &&
                x.ParticipanteIdentidadId == ParticipanteId &&
                x.EquipoId == null),
            It.IsAny<CancellationToken>()), Times.Once);
        arr.Notificador.Verify(n => n.NotificarRespuestaRegistradaAsync(
            sesion.Id, EtapaId, PreguntaId, ParticipanteId, null,
            true, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (2)
    public async Task Individual_SegundoIntentoMismoParticipante_EsRechazado()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, existeRespuesta: true);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        (await accion.Should().ThrowAsync<RespuestaTriviaDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeFalse();
        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.IsAny<RespuestaTriviaRegistro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (3)
    public async Task Individual_OtroParticipanteRespondeLaMismaPregunta_Independiente()
    {
        var sesion = IndividualActiva(participanteExtra: OtroParticipante);
        // El otro participante aún no respondió → existeRespuesta=false.
        var arr = new Arranque(sesion, OtroParticipante);

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x =>
                x.ParticipanteIdentidadId == OtroParticipante && x.EquipoId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (4)
    public async Task Individual_PuntajeSeCalculaConLaEstrategiaInyectada()
    {
        var sesion = IndividualActiva();
        var estrategia = new Mock<IEstrategiaCalculoPuntajeTrivia>();
        estrategia.Setup(e => e.Calcular(It.IsAny<ContextoCalculoPuntajeTrivia>())).Returns(777);
        var arr = new Arranque(sesion, ParticipanteId, verificacion: Verif(puntaje: 100))
        {
            Estrategia = estrategia.Object
        };

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.PuntosGanados.Should().Be(777);
        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x => x.PuntosGanados == 777),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (5 / 26) El manejador delega en la abstracción; no contiene el algoritmo.
    public async Task Individual_ManejadorDelegaEnLaEstrategiaConElContextoCorrecto()
    {
        var sesion = IndividualActiva();
        var estrategia = new Mock<IEstrategiaCalculoPuntajeTrivia>();
        estrategia.Setup(e => e.Calcular(It.IsAny<ContextoCalculoPuntajeTrivia>())).Returns(5);
        var arr = new Arranque(sesion, ParticipanteId,
            verificacion: Verif(correcta: true, puntaje: 5, limite: 10))
        {
            Estrategia = estrategia.Object
        };

        await arr.EjecutarAsync(sesion.Id, tiempoMs: 3000);

        estrategia.Verify(e => e.Calcular(It.Is<ContextoCalculoPuntajeTrivia>(c =>
            c.EsCorrecta &&
            c.PuntajeBase == 5 &&
            c.TiempoTardadoMs == 1000 &&
            c.TiempoLimiteMs == 10_000)), Times.Once);
    }

    [Fact]
    public async Task Individual_IgnoraTiempoTardadoDelCliente_YUsaTiempoDelServidor()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId,
            verificacion: Verif(correcta: true, puntaje: 100, limite: 10));
        arr.Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc.AddSeconds(4));

        await arr.EjecutarAsync(sesion.Id, tiempoMs: 1);

        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x => x.TiempoTardadoMs == 4_000),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Individual_RespuestaCorrecta_Tramo0_DevuelvePuntajeCompleto()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, verificacion: Verif(puntaje: 100, limite: 10));

        var resultado = await arr.EjecutarAsync(sesion.Id, tiempoMs: 0);

        resultado.PuntosGanados.Should().Be(100);
    }

    [Fact]
    public async Task Individual_RespuestaIncorrecta_DevuelveCeroPuntos()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, verificacion: Verif(correcta: false));

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EsCorrecta.Should().BeFalse();
        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task Individual_TimeoutSinOpcion_PersisteNuloIncorrectaYCeroPuntos()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);

        var resultado = await arr.EjecutarTimeoutAsync(sesion.Id);

        resultado.EsCorrecta.Should().BeFalse();
        resultado.PuntosGanados.Should().Be(0);
        arr.ClienteTrivia.Verify(c => c.VerificarRespuestaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x =>
                x.OpcionSeleccionadaId == null &&
                !x.EsCorrecta &&
                x.PuntosGanados == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ======================================================================
    // SESIÓN GRUPAL
    // ======================================================================

    [Fact] // (6, 12, 13) Primer integrante registra; se conserva autor y equipo.
    public async Task Grupal_PrimerIntegranteResponde_PersisteConEquipoYAutor()
    {
        var (sesion, rojoId, _) = GrupalActiva();
        var arr = new Arranque(sesion, Ana);

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x =>
                x.ParticipanteIdentidadId == Ana &&   // autor real
                x.EquipoId == rojoId),                 // jugador lógico = equipo
            It.IsAny<CancellationToken>()), Times.Once);
        arr.Notificador.Verify(n => n.NotificarRespuestaRegistradaAsync(
            sesion.Id, EtapaId, PreguntaId, Ana, rojoId,
            It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (7, 8, 9, 10) Segundo integrante rechazado; sin puntos, sin persistir, sin 2º evento.
    public async Task Grupal_SegundoIntegranteMismoEquipo_EsRechazado()
    {
        var (sesion, _, _) = GrupalActiva();
        // El equipo ya tiene respuesta oficial → existeRespuesta=true.
        var arr = new Arranque(sesion, Pedro, existeRespuesta: true);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        (await accion.Should().ThrowAsync<RespuestaTriviaDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeTrue();
        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.IsAny<RespuestaTriviaRegistro>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.Notificador.Verify(n => n.NotificarRespuestaRegistradaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (11) Otro equipo sí puede responder la misma pregunta.
    public async Task Grupal_OtroEquipoPuedeResponderLaMismaPregunta()
    {
        var (sesion, _, azulId) = GrupalActiva();
        var arr = new Arranque(sesion, Beto); // Beto es del equipo Azul
        arr.RepoRespuestas.Setup(r => r.ExisteRespuestaOficialAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), azulId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x =>
                x.ParticipanteIdentidadId == Beto && x.EquipoId == azulId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ======================================================================
    // FINALIZACIÓN DE ETAPA
    // ======================================================================

    [Fact] // (14) Registros duplicados sobre menos preguntas distintas NO completan la etapa.
    public async Task Grupal_EtapaNoSeCompletaSiFaltanPreguntasDistintas()
    {
        var (sesion, _, _) = GrupalActiva();
        // Total 5; el equipo solo tiene 2 preguntas distintas respondidas.
        var arr = new Arranque(sesion, Ana, totalPreguntas: 5, preguntasDistintas: 2);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeFalse();
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (16) La etapa grupal solo se completa cuando TODOS los equipos terminaron.
    public async Task Grupal_EtapaNoSeCompletaSiFaltaAlgunEquipo()
    {
        var (sesion, _, _) = GrupalActiva(); // 2 equipos
        var arr = new Arranque(sesion, Ana,
            totalPreguntas: 5, preguntasDistintas: 5, jugadoresCompletaron: 1);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeFalse();
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (15, 16) Se completa cuando todos los equipos respondieron todas las preguntas.
    public async Task Grupal_EtapaSeCompletaCuandoTodosLosEquiposTerminan()
    {
        var (sesion, _, _) = GrupalActiva(); // 2 equipos
        var arr = new Arranque(sesion, Ana,
            totalPreguntas: 5, preguntasDistintas: 5, jugadoresCompletaron: 2);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeTrue();
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, MisionId, EtapaId, It.IsAny<CancellationToken>()), Times.Never);
        // Todos completaron ⇒ NO se cierra de inmediato: se entra en CierrePendiente
        // (feedback final) y el worker cierra al vencer.
        arr.Finalizacion.Verify(s => s.ProgramarCierreTrasFeedbackAsync(
            sesion.Id, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        arr.Finalizacion.Verify(s => s.FinalizarSiTodasEtapasCompletadasAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (17) Individual: la etapa se completa cuando el único participante termina.
    public async Task Individual_EtapaSeCompletaCuandoElParticipanteTermina()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId,
            totalPreguntas: 3, preguntasDistintas: 3, jugadoresCompletaron: 1);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeTrue();
    }

    [Fact] // (14, seguridad) El total lo aporta juegos-servicio, no el cliente.
    public async Task Total_DePreguntasSeObtieneDeJuegosServicio_NoDelCliente()
    {
        var sesion = IndividualActiva();
        // La trivia real tiene 5 preguntas; el jugador respondió solo 1 distinta.
        var arr = new Arranque(sesion, ParticipanteId, totalPreguntas: 5, preguntasDistintas: 1);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeFalse();
        arr.ClienteTrivia.Verify(c => c.ObtenerTriviaParticipanteAsync(
            TriviaId, It.IsAny<CancellationToken>()), Times.Once);
        // El conteo de jugadores completados usa el total confiable (5), no un valor del cliente.
        arr.RepoRespuestas.Verify(r => r.ContarJugadoresQueCompletaronEtapaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never); // ni siquiera llega a contar (1 < 5)
    }

    // ======================================================================
    // CONCURRENCIA (condición de carrera)
    // ======================================================================

    [Fact] // (18, 20) Si la BD rechaza la 2ª inserción, no hay 2º evento ni puntos ni 500.
    public async Task Concurrencia_InsercionDuplicada_SeConvierteEnConflictoDeNegocio()
    {
        var (sesion, _, _) = GrupalActiva();
        var arr = new Arranque(sesion, Pedro); // pasó el pre-chequeo (existeRespuesta=false)
        // Simula la violación del índice único filtrado traducida por el repositorio.
        arr.RepoRespuestas.Setup(r => r.AgregarAsync(
                It.IsAny<RespuestaTriviaRegistro>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RespuestaTriviaDuplicadaExcepcion(esEquipo: true));

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        (await accion.Should().ThrowAsync<RespuestaTriviaDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeTrue();
        arr.Notificador.Verify(n => n.NotificarRespuestaRegistradaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.RepoRespuestas.Verify(r => r.ContarJugadoresQueCompletaronEtapaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // COHERENCIA / ERRORES
    // ======================================================================

    [Fact]
    public async Task MisionQueNoPerteneceALaSesion_EsRechazada()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);

        Func<Task> accion = () => arr.Construir().Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, Guid.NewGuid(), EtapaId, TriviaId, PreguntaId, OpcionId, 0),
            CancellationToken.None);

        await accion.Should().ThrowAsync<MisionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task UsuarioNoAutenticado_LanzaUnauthorizedAccessException()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);
        arr.Usuario.Setup(u => u.ObtenerId()).Returns((Guid?)null);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SesionNoEncontrada_LanzaSesionNoEncontradaExcepcion()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);
        arr.RepoSesiones.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => arr.EjecutarAsync(Guid.NewGuid());

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task SesionNoActiva_LanzaOperacionSesionInvalida()
    {
        var sesion = SesionIndividual.Crear(
            "Trivia", "Demo", AhoraUtc.AddHours(1), "TRIV01", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new[] { MisionId });
        sesion.Preparar(); // EnPreparacion, no Activa
        var arr = new Arranque(sesion, ParticipanteId);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task ParticipanteNoInscrito_LanzaParticipacionInvalida()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, Guid.NewGuid()); // usuario ajeno a la sesión

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public async Task VerificacionNula_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);
        arr.ClienteTrivia.Setup(c => c.VerificarRespuestaAsync(
                TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificacionRespuestaJuegosDto?)null);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrada*");
    }
}
