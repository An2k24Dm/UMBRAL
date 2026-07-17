using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;
using SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class EnviarEvidenciaTesoroManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtroParticipante = Guid.Parse("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid BusquedaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string CodigoValido = "QR-TESORO-001";

    // Integrantes de sesión grupal.
    private static readonly Guid Ana = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");   // Rojo
    private static readonly Guid Pedro = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"); // Rojo
    private static readonly Guid Carlos = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3"); // Azul
    private static readonly Guid Maria = Guid.Parse("d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4");  // Azul

    // ----------------------------------------------------------------------
    // Constructores de sesiones
    // ----------------------------------------------------------------------

    // Duración de la etapa de tesoro en las pruebas (ms = 300_000).
    private const int DuracionEtapaSegundos = 300;

    private static SesionIndividual IndividualActiva(Guid? participanteExtra = null)
    {
        var s = SesionIndividual.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new[] { MisionId });
        s.Preparar();
        s.AgregarParticipante(ParticipanteId, AhoraUtc);
        if (participanteExtra.HasValue) s.AgregarParticipante(participanteExtra.Value, AhoraUtc);
        s.IniciarPrimeraEtapa(
            MisionId, EtapaId, BusquedaId, "BusquedaTesoro", 1, AhoraUtc, DuracionEtapaSegundos);
        return s;
    }

    private static (SesionGrupal sesion, Guid rojoId, Guid azulId) GrupalActiva()
    {
        var s = SesionGrupal.Crear(
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);
        s.AsignarMisiones(new[] { MisionId });
        s.Preparar();
        var rojo = s.CrearEquipo("Rojo", Ana, AhoraUtc, AhoraUtc);
        s.AgregarParticipanteAEquipo(rojo.Id, Pedro, AhoraUtc, AhoraUtc);
        var azul = s.CrearEquipo("Azul", Carlos, AhoraUtc, AhoraUtc);
        s.AgregarParticipanteAEquipo(azul.Id, Maria, AhoraUtc, AhoraUtc);
        s.IniciarPrimeraEtapa(
            MisionId, EtapaId, BusquedaId, "BusquedaTesoro", 1, AhoraUtc, DuracionEtapaSegundos);
        return (s, rojo.Id, azul.Id);
    }

    // ----------------------------------------------------------------------
    // Arranque configurable del manejador con dobles
    // ----------------------------------------------------------------------

    private sealed class Arranque
    {
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IRepositorioSesiones> RepoSesiones { get; } = new();
        public Mock<IClienteBusquedaTesoro> ClienteTesoro { get; } = new();
        public Mock<IRepositorioEvidenciasTesoro> RepoEvidencias { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IServicioFinalizacionSesion> Finalizacion { get; } = new();
        public Mock<IServicioProgresoSecuencialSesion> ProgresoSecuencial { get; } = new();
        public Mock<IPublicadorEventosRanking> PublicadorRanking { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Mock<IUnidadTrabajoSesiones> UnidadTrabajo { get; } = new();

        public Arranque(
            Sesion sesion,
            Guid pid,
            bool yaCompletado = false,
            bool esValida = true,
            int puntajeBase = 50,
            int participantesCompletaron = 0,
            int equiposCompletaron = 0)
        {
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            RepoEvidencias.Setup(r => r.BloquearEtapaParaOrdenAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Usuario.Setup(u => u.ObtenerId()).Returns(pid);
            RepoSesiones.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            RepoEvidencias.Setup(r => r.ExisteEvidenciaValidaIndividualAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(yaCompletado);
            RepoEvidencias.Setup(r => r.ExisteEvidenciaValidaEquipoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(yaCompletado);

            ProgresoSecuencial.Setup(s => s.ValidarEtapaActualAsync(
                    It.IsAny<Sesion>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            PublicadorRanking.Setup(p => p.PublicarEvidenciaTesoroRegistradaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<bool>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            ClienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                    BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
                .ReturnsAsync(esValida);
            ClienteTesoro.Setup(c => c.ObtenerBusquedaParticipanteAsync(
                    BusquedaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BusquedaTesoroJuegosDto { Puntaje = puntajeBase });

            RepoEvidencias.Setup(r => r.AgregarAsync(
                    It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            RepoEvidencias.Setup(r => r.ContarParticipantesConEvidenciaValidaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(participantesCompletaron);
            RepoEvidencias.Setup(r => r.ContarEquiposConEvidenciaValidaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(equiposCompletaron);

            Notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarProgresoSecuencialActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Finalizacion.Setup(s => s.ProgramarCierreTrasFeedbackAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnidadTrabajo.Setup(u => u.EjecutarEnTransaccionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));
        }

        // Construye el manejador con la CADENA REAL de validación (Chain of
        // Responsibility) armada sobre los mismos dobles, de modo que las pruebas
        // del manejador siguen verificando el comportamiento de extremo a extremo.
        public EnviarEvidenciaTesoroManejador Construir()
        {
            var fabricaCadena = new FabricaCadenaValidacionEvidenciaTesoro(
                new EslabonSesionActiva(RepoSesiones.Object),
                new EslabonParticipanteInscrito(),
                new EslabonEtapaActual(ProgresoSecuencial.Object),
                new EslabonEvidenciaNoDuplicada(RepoEvidencias.Object),
                new EslabonCodigoQr(ClienteTesoro.Object));

            return new(
                Usuario.Object, fabricaCadena, ClienteTesoro.Object,
                RepoEvidencias.Object, Notificador.Object, Finalizacion.Object,
                PublicadorRanking.Object, Reloj.Object, UnidadTrabajo.Object);
        }

        public Task<EvidenciaTesoroRespuestaDto> EjecutarAsync(Guid sesionId)
            => Construir().Handle(
                new EnviarEvidenciaTesoroComando(sesionId, MisionId, EtapaId, BusquedaId, CodigoValido),
                CancellationToken.None);
    }

    // ======================================================================
    // SESIÓN INDIVIDUAL
    // ======================================================================

    [Fact] // (1)
    public async Task Individual_QrValido_RegistraEvidenciaConEquipoNullYEvento()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, puntajeBase: 75);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EsValida.Should().BeTrue();
        resultado.EventoId.Should().NotBe(Guid.Empty);
        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.Is<EvidenciaTesoroRegistro>(x =>
                x.SesionId == sesion.Id &&
                x.ParticipanteIdentidadId == ParticipanteId &&
                x.EquipoId == null &&
                x.EsValida &&
                x.PuntosGanados == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (2)
    public async Task Individual_OtroParticipanteCompletaIndependientemente()
    {
        var sesion = IndividualActiva(participanteExtra: OtroParticipante);
        var arr = new Arranque(sesion, OtroParticipante); // aún no completó

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.Is<EvidenciaTesoroRegistro>(x =>
                x.ParticipanteIdentidadId == OtroParticipante && x.EquipoId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (3)
    public async Task Individual_MismoParticipanteDosEvidenciasValidas_EsRechazado()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, yaCompletado: true);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        (await accion.Should().ThrowAsync<EvidenciaTesoroDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeFalse();
        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (4)
    public async Task Individual_QrInvalido_NoCompletaEtapa_PeroRegistraIntento()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, esValida: false);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EsValida.Should().BeFalse();
        resultado.EventoId.Should().NotBe(Guid.Empty);
        resultado.EtapaCompletada.Should().BeFalse();
        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.Is<EvidenciaTesoroRegistro>(x => !x.EsValida && x.PuntosGanados == 0),
            It.IsAny<CancellationToken>()), Times.Once);
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (§13) QR inválido: no toma bloqueo ni ocupa posición (orden 0).
    public async Task Individual_QrInvalido_NoTomaBloqueoNiOrden()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId, esValida: false, puntajeBase: 40);

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoEvidencias.Verify(r => r.BloquearEtapaParaOrdenAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.PublicadorRanking.Verify(p => p.PublicarEvidenciaTesoroRegistradaAsync(
            It.IsAny<Guid>(), sesion.Id, MisionId, EtapaId,
            It.IsAny<Guid>(), ParticipanteId, null, BusquedaId,
            false,      // EsValida
            40,         // PuntajeBase
            0,          // OrdenResolucion (no ocupa posición)
            1,          // TotalCompetidores (1 participante)
            0,          // TiempoTranscurridoMs
            300_000,    // TiempoLimiteMs
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Individual_EtapaVencida_RechazaEvidencia()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);
        arr.Reloj.Setup(r => r.ObtenerFechaHoraUtc())
            .Returns(AhoraUtc.AddSeconds(DuracionEtapaSegundos + 1));

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>()
            .WithMessage("*tiempo de la etapa*");
        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // SESIÓN GRUPAL
    // ======================================================================

    [Fact] // (5) Primer integrante del equipo registra evidencia con equipo y autor.
    public async Task Grupal_PrimerIntegranteRojo_RegistraConEquipoYAutor()
    {
        var (sesion, rojoId, _) = GrupalActiva();
        // Tras insertar la evidencia válida, el conteo de equipos = 1 (Rojo primero).
        var arr = new Arranque(sesion, Ana, equiposCompletaron: 1);

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.Is<EvidenciaTesoroRegistro>(x =>
                x.ParticipanteIdentidadId == Ana &&  // autor real
                x.EquipoId == rojoId &&               // jugador lógico = equipo
                x.EsValida),
            It.IsAny<CancellationToken>()), Times.Once);
        // El orden se asigna bajo bloqueo, antes de publicar.
        arr.RepoEvidencias.Verify(r => r.BloquearEtapaParaOrdenAsync(
            sesion.Id, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        arr.PublicadorRanking.Verify(p => p.PublicarEvidenciaTesoroRegistradaAsync(
            It.IsAny<Guid>(),
            sesion.Id,
            MisionId,
            EtapaId,
            It.IsAny<Guid>(),
            Ana,
            rojoId,
            BusquedaId,
            true,
            50,
            1,          // OrdenResolucion (primer equipo válido)
            2,          // TotalCompetidores (2 equipos)
            0,          // TiempoTranscurridoMs (inicio == ahora)
            300_000,    // TiempoLimiteMs (300 s)
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (6) Segundo integrante del mismo equipo es rechazado, sin puntos ni progreso.
    public async Task Grupal_SegundoIntegranteRojo_EsRechazado()
    {
        var (sesion, _, _) = GrupalActiva();
        var arr = new Arranque(sesion, Pedro, yaCompletado: true); // Rojo ya tiene evidencia válida

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        (await accion.Should().ThrowAsync<EvidenciaTesoroDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeTrue();
        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.RepoEvidencias.Verify(r => r.ContarEquiposConEvidenciaValidaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (7) Otro equipo puede completar independientemente.
    public async Task Grupal_EquipoAzulCompletaIndependientemente()
    {
        var (sesion, _, azulId) = GrupalActiva();
        var arr = new Arranque(sesion, Carlos); // Azul, aún no completó

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoEvidencias.Verify(r => r.AgregarAsync(
            It.Is<EvidenciaTesoroRegistro>(x =>
                x.ParticipanteIdentidadId == Carlos && x.EquipoId == azulId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (11) El progreso grupal usa el conteo de EQUIPOS, no de participantes.
    public async Task Grupal_UsaConteoDeEquipos_NoDeParticipantes()
    {
        var (sesion, _, _) = GrupalActiva();
        var arr = new Arranque(sesion, Ana, equiposCompletaron: 1);

        await arr.EjecutarAsync(sesion.Id);

        arr.RepoEvidencias.Verify(r => r.ContarEquiposConEvidenciaValidaAsync(
            sesion.Id, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        arr.RepoEvidencias.Verify(r => r.ContarParticipantesConEvidenciaValidaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (8, 9) Con 2 equipos, solo Rojo completado → etapa NO completada.
    public async Task Grupal_UnEquipoCompletadoDeDos_NoCompletaEtapa()
    {
        var (sesion, _, _) = GrupalActiva(); // 2 equipos
        // Aunque dos integrantes de Rojo tuvieran evidencia, COUNT(DISTINCT equipo)=1.
        var arr = new Arranque(sesion, Ana, equiposCompletaron: 1);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeFalse();
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        arr.Finalizacion.Verify(s => s.ProgramarCierreTrasFeedbackAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (10, 17) Cuando todos los equipos completan → etapa completada y finalización.
    public async Task Grupal_TodosLosEquiposCompletan_CompletaEtapaYFinaliza()
    {
        var (sesion, _, _) = GrupalActiva(); // 2 equipos
        var arr = new Arranque(sesion, Carlos, equiposCompletaron: 2);

        var resultado = await arr.EjecutarAsync(sesion.Id);

        resultado.EtapaCompletada.Should().BeTrue();
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, MisionId, EtapaId, It.IsAny<CancellationToken>()), Times.Never);
        // Último equipo completó ⇒ cierre pendiente (feedback final), no cierre inmediato.
        arr.Finalizacion.Verify(s => s.ProgramarCierreTrasFeedbackAsync(
            sesion.Id, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ======================================================================
    // CONCURRENCIA
    // ======================================================================

    [Fact] // (12, 14) Inserción duplicada por carrera → conflicto de negocio, sin 2ª notificación.
    public async Task Concurrencia_InsercionDuplicada_SeConvierteEnConflicto()
    {
        var (sesion, _, _) = GrupalActiva();
        var arr = new Arranque(sesion, Pedro); // pasó el pre-chequeo (yaCompletado=false)
        arr.RepoEvidencias.Setup(r => r.AgregarAsync(
                It.IsAny<EvidenciaTesoroRegistro>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EvidenciaTesoroDuplicadaExcepcion(esEquipo: true));

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        (await accion.Should().ThrowAsync<EvidenciaTesoroDuplicadaExcepcion>())
            .Which.EsEquipo.Should().BeTrue();
        arr.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        arr.RepoEvidencias.Verify(r => r.ContarEquiposConEvidenciaValidaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // ERRORES
    // ======================================================================

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
            "Tesoro", "Demo", AhoraUtc.AddHours(1), "TESO01", Operador, AhoraUtc, 5);
        sesion.Preparar(); // EnPreparacion, no Activa
        var arr = new Arranque(sesion, ParticipanteId);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task ParticipanteNoInscrito_LanzaParticipacionInvalida()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, Guid.NewGuid()); // usuario ajeno

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public async Task BusquedaNoEncontrada_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var arr = new Arranque(sesion, ParticipanteId);
        arr.ClienteTesoro.Setup(c => c.ValidarCodigoQrAsync(
                BusquedaId, CodigoValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool?)null);

        Func<Task> accion = () => arr.EjecutarAsync(sesion.Id);

        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrada*");
    }
}
