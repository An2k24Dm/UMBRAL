using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class EnviarRespuestaTriviaManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SesionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid TriviaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid PreguntaId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid OpcionId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    private static SesionIndividual IndividualActiva()
    {
        var s = SesionIndividual.Crear(
            "Trivia", "Demo", AhoraUtc.AddHours(1), "TRIV01", Operador, AhoraUtc, 5);
        s.Preparar();
        s.AgregarParticipante(ParticipanteId, AhoraUtc);
        s.Iniciar(AhoraUtc);
        return s;
    }

    private sealed class Contexto
    {
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IClienteJuegosTrivia> ClienteTrivia { get; } = new();
        public Mock<IRepositorioRespuestasTrivia> RepoRespuestas { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IServicioFinalizacionSesion> ServicioFinalizacion { get; } = new();

        public Contexto(
            Sesion sesion,
            Guid? participanteIdentidadId = null,
            bool yaRespondio = false,
            VerificacionRespuestaJuegosDto? verificacion = null,
            int respuestasDelJugador = 0,
            int jugadoresCompletaron = 0,
            int totalPreguntas = 1)
        {
            var pid = participanteIdentidadId ?? ParticipanteId;

            Usuario.Setup(u => u.ObtenerId()).Returns(pid);
            Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            RepoRespuestas.Setup(r => r.ExisteRespuestaAsync(
                    sesion.Id, EtapaId, PreguntaId, pid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(yaRespondio);

            var ver = verificacion ?? new VerificacionRespuestaJuegosDto
            {
                EsCorrecta = true,
                PuntajeBase = 100,
                TiempoLimiteSegundos = 10
            };
            ClienteTrivia.Setup(c => c.VerificarRespuestaAsync(
                    TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ver);

            RepoRespuestas.Setup(r => r.AgregarAsync(
                    It.IsAny<RespuestaTriviaRegistro>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Notificador.Setup(n => n.NotificarRespuestaRegistradaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            RepoRespuestas.Setup(r => r.ContarRespuestasDeJugadorEnEtapaAsync(
                    sesion.Id, EtapaId, pid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(respuestasDelJugador);

            RepoRespuestas.Setup(r => r.ContarJugadoresQueCompletaronEtapaAsync(
                    sesion.Id, EtapaId, totalPreguntas, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jugadoresCompletaron);

            Notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            ServicioFinalizacion.Setup(s => s.FinalizarSiTodasEtapasCompletadasAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public EnviarRespuestaTriviaManejador Construir()
            => new(
                Usuario.Object,
                Repo.Object,
                ClienteTrivia.Object,
                RepoRespuestas.Object,
                Notificador.Object,
                ServicioFinalizacion.Object);

        public Task<EnviarRespuestaTriviaRespuesta> Ejecutar(
            int tiempoTardadoMs = 0, int totalPreguntas = 1)
            => Construir().Handle(
                new EnviarRespuestaTriviaComando(
                    IndividualActiva().Id == SesionId ? SesionId : IndividualActiva().Id,
                    MisionId, EtapaId, TriviaId, PreguntaId, OpcionId,
                    tiempoTardadoMs, totalPreguntas),
                CancellationToken.None);
    }

    private Task<EnviarRespuestaTriviaRespuesta> EjecutarConSesion(
        SesionIndividual sesion,
        VerificacionRespuestaJuegosDto? verificacion = null,
        int tiempoTardadoMs = 0,
        int totalPreguntas = 1,
        int respuestasDelJugador = 1,
        int jugadoresCompletaron = 0)
    {
        var ctx = new Contexto(
            sesion,
            verificacion: verificacion,
            respuestasDelJugador: respuestasDelJugador,
            jugadoresCompletaron: jugadoresCompletaron,
            totalPreguntas: totalPreguntas);
        return ctx.Construir().Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId,
                tiempoTardadoMs, totalPreguntas),
            CancellationToken.None);
    }

    [Fact]
    public async Task RespuestaCorrecta_Tramo0_DevuelvePuntajeCompleto()
    {
        var sesion = IndividualActiva();
        var verificacion = new VerificacionRespuestaJuegosDto
            { EsCorrecta = true, PuntajeBase = 100, TiempoLimiteSegundos = 10 };
        // 0ms tardado → tramo 0 → 100 * (1 - 0*0.2) = 100
        var resultado = await EjecutarConSesion(sesion, verificacion, tiempoTardadoMs: 0);

        resultado.EsCorrecta.Should().BeTrue();
        resultado.PuntosGanados.Should().Be(100);
    }

    [Theory]
    [InlineData(1000, 100)]  // tramo 0 (0-2000ms): 100%
    [InlineData(2001, 80)]   // tramo 1 (2001-4000ms): 80%
    [InlineData(4001, 60)]   // tramo 2 (4001-6000ms): 60%
    [InlineData(6001, 39)]   // tramo 3 (6001-8000ms): float truncation → 39
    [InlineData(8001, 19)]   // tramo 4 (8001-9999ms): float truncation → 19
    public async Task CalcularPuntaje_SegúnTramoDeTiempo(int tiempoMs, int puntajeEsperado)
    {
        var sesion = IndividualActiva();
        var verificacion = new VerificacionRespuestaJuegosDto
            { EsCorrecta = true, PuntajeBase = 100, TiempoLimiteSegundos = 10 };

        var resultado = await EjecutarConSesion(sesion, verificacion, tiempoTardadoMs: tiempoMs);

        resultado.PuntosGanados.Should().Be(puntajeEsperado);
    }

    [Fact]
    public async Task RespuestaIncorrecta_DevuelveCeroPuntos()
    {
        var sesion = IndividualActiva();
        var verificacion = new VerificacionRespuestaJuegosDto
            { EsCorrecta = false, PuntajeBase = 100, TiempoLimiteSegundos = 10 };

        var resultado = await EjecutarConSesion(sesion, verificacion);

        resultado.EsCorrecta.Should().BeFalse();
        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task TiempoTardadoIgualAlLimite_DevuelveCeroPuntos()
    {
        var sesion = IndividualActiva();
        var verificacion = new VerificacionRespuestaJuegosDto
            { EsCorrecta = true, PuntajeBase = 100, TiempoLimiteSegundos = 10 };
        // tiempoLimiteMs = 10000, tiempoTardadoMs = 10000 → >= → 0
        var resultado = await EjecutarConSesion(sesion, verificacion, tiempoTardadoMs: 10_000);

        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task TiempoLimiteCero_DevuelveCeroPuntos()
    {
        var sesion = IndividualActiva();
        var verificacion = new VerificacionRespuestaJuegosDto
            { EsCorrecta = true, PuntajeBase = 100, TiempoLimiteSegundos = 0 };

        var resultado = await EjecutarConSesion(sesion, verificacion);

        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task RespuestaCorrecta_PersisteLaRespuesta()
    {
        var sesion = IndividualActiva();
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        SetupRepoRespuestasDefault(repoRespuestas);
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var clienteTrivia = new Mock<IClienteJuegosTrivia>();
        clienteTrivia.Setup(c => c.VerificarRespuestaAsync(
                TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificacionRespuestaJuegosDto
                { EsCorrecta = true, PuntajeBase = 50, TiempoLimiteSegundos = 10 });
        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarRespuestaRegistradaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var manejador = new EnviarRespuestaTriviaManejador(
            usuario.Object, repo.Object, clienteTrivia.Object,
            repoRespuestas.Object, notificador.Object, Mock.Of<IServicioFinalizacionSesion>());

        await manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        repoRespuestas.Verify(r => r.AgregarAsync(
            It.Is<RespuestaTriviaRegistro>(x =>
                x.SesionId == sesion.Id &&
                x.PreguntaId == PreguntaId &&
                x.ParticipanteIdentidadId == ParticipanteId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RespuestaCorrecta_NotificaRespuestaRegistrada()
    {
        var sesion = IndividualActiva();
        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarRespuestaRegistradaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var manejador = ConstruirManejadorMinimo(sesion, notificador);

        await manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        notificador.Verify(n => n.NotificarRespuestaRegistradaAsync(
            sesion.Id, EtapaId, PreguntaId, ParticipanteId, null,
            true, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EtapaCompletada_CuandoTodosCompletaron_NotificaYLlamaFinalizacion()
    {
        var sesion = IndividualActiva();
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        SetupRepoRespuestasDefault(repoRespuestas);
        repoRespuestas.Setup(r => r.ContarRespuestasDeJugadorEnEtapaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // >= totalPreguntas(1)
        repoRespuestas.Setup(r => r.ContarJugadoresQueCompletaronEtapaAsync(
                sesion.Id, EtapaId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // >= participantes(1)
        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarRespuestaRegistradaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notificador.Setup(n => n.NotificarEtapaCompletadaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var servicioFinalizacion = new Mock<IServicioFinalizacionSesion>();
        servicioFinalizacion.Setup(s => s.FinalizarSiTodasEtapasCompletadasAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var repo = BuildRepo(sesion);
        var manejador = new EnviarRespuestaTriviaManejador(
            BuildUsuario(), repo, BuildClienteTrivia(),
            repoRespuestas.Object, notificador.Object, servicioFinalizacion.Object);

        var resultado = await manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        resultado.EtapaCompletada.Should().BeTrue();
        notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, MisionId, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        servicioFinalizacion.Verify(s => s.FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EtapaNoCompletada_CuandoNoTodosCompletaron_NoNotificaEtapaCompletada()
    {
        var sesion = IndividualActiva();
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        SetupRepoRespuestasDefault(repoRespuestas);
        repoRespuestas.Setup(r => r.ContarRespuestasDeJugadorEnEtapaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // jugador no completó aún
        var notificador = new Mock<INotificadorSesionesTiempoReal>();
        notificador.Setup(n => n.NotificarRespuestaRegistradaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var repo = BuildRepo(sesion);
        var manejador = new EnviarRespuestaTriviaManejador(
            BuildUsuario(), repo, BuildClienteTrivia(),
            repoRespuestas.Object, notificador.Object, Mock.Of<IServicioFinalizacionSesion>());

        var resultado = await manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        resultado.EtapaCompletada.Should().BeFalse();
        notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UsuarioNoAutenticado_LanzaUnauthorizedAccessException()
    {
        var sesion = IndividualActiva();
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns((Guid?)null);
        var manejador = new EnviarRespuestaTriviaManejador(
            usuario.Object, Mock.Of<IRepositorioSesiones>(),
            Mock.Of<IClienteJuegosTrivia>(), Mock.Of<IRepositorioRespuestasTrivia>(),
            Mock.Of<INotificadorSesionesTiempoReal>(), Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        await accion.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SesionNoEncontrada_LanzaInvalidOperationException()
    {
        var repo = new Mock<IRepositorioSesiones>();
        repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var manejador = new EnviarRespuestaTriviaManejador(
            usuario.Object, repo.Object, Mock.Of<IClienteJuegosTrivia>(),
            Mock.Of<IRepositorioRespuestasTrivia>(), Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarRespuestaTriviaComando(
                Guid.NewGuid(), MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task SesionNoActiva_LanzaInvalidOperationException()
    {
        var sesion = SesionIndividual.Crear(
            "Trivia", "Demo", AhoraUtc.AddHours(1), "TRIV01", Operador, AhoraUtc, 5);
        sesion.Preparar();
        // Estado: EnPreparacion (no Activa)
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        var repo = BuildRepo(sesion);
        var manejador = new EnviarRespuestaTriviaManejador(
            usuario.Object, repo, Mock.Of<IClienteJuegosTrivia>(),
            Mock.Of<IRepositorioRespuestasTrivia>(), Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está activa*");
    }

    [Fact]
    public async Task ParticipanteNoInscrito_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var otroUsuario = Guid.NewGuid();
        var usuario = new Mock<IUsuarioActual>();
        usuario.Setup(u => u.ObtenerId()).Returns(otroUsuario);
        var repo = BuildRepo(sesion);
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        repoRespuestas.Setup(r => r.ExisteRespuestaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var manejador = new EnviarRespuestaTriviaManejador(
            usuario.Object, repo, Mock.Of<IClienteJuegosTrivia>(),
            repoRespuestas.Object, Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no está inscrito*");
    }

    [Fact]
    public async Task YaRespondio_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        repoRespuestas.Setup(r => r.ExisteRespuestaAsync(
                sesion.Id, EtapaId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var manejador = new EnviarRespuestaTriviaManejador(
            BuildUsuario(), BuildRepo(sesion), Mock.Of<IClienteJuegosTrivia>(),
            repoRespuestas.Object, Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya respondiste*");
    }

    [Fact]
    public async Task VerificacionNula_LanzaInvalidOperationException()
    {
        var sesion = IndividualActiva();
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        repoRespuestas.Setup(r => r.ExisteRespuestaAsync(
                sesion.Id, EtapaId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var clienteTrivia = new Mock<IClienteJuegosTrivia>();
        clienteTrivia.Setup(c => c.VerificarRespuestaAsync(
                TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VerificacionRespuestaJuegosDto?)null);
        var manejador = new EnviarRespuestaTriviaManejador(
            BuildUsuario(), BuildRepo(sesion), clienteTrivia.Object,
            repoRespuestas.Object, Mock.Of<INotificadorSesionesTiempoReal>(),
            Mock.Of<IServicioFinalizacionSesion>());

        Func<Task> accion = () => manejador.Handle(
            new EnviarRespuestaTriviaComando(
                sesion.Id, MisionId, EtapaId, TriviaId, PreguntaId, OpcionId, 0, 1),
            CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no encontrada*");
    }

    // Helpers

    private static IUsuarioActual BuildUsuario()
    {
        var u = new Mock<IUsuarioActual>();
        u.Setup(x => x.ObtenerId()).Returns(ParticipanteId);
        return u.Object;
    }

    private static IRepositorioSesiones BuildRepo(Sesion sesion)
    {
        var r = new Mock<IRepositorioSesiones>();
        r.Setup(x => x.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        return r.Object;
    }

    private static IClienteJuegosTrivia BuildClienteTrivia()
    {
        var c = new Mock<IClienteJuegosTrivia>();
        c.Setup(x => x.VerificarRespuestaAsync(
                TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificacionRespuestaJuegosDto
                { EsCorrecta = true, PuntajeBase = 100, TiempoLimiteSegundos = 10 });
        return c.Object;
    }

    private static void SetupRepoRespuestasDefault(Mock<IRepositorioRespuestasTrivia> repo)
    {
        repo.Setup(r => r.ExisteRespuestaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AgregarAsync(
                It.IsAny<RespuestaTriviaRegistro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.ContarRespuestasDeJugadorEnEtapaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repo.Setup(r => r.ContarJugadoresQueCompletaronEtapaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    private EnviarRespuestaTriviaManejador ConstruirManejadorMinimo(
        Sesion sesion, Mock<INotificadorSesionesTiempoReal> notificador)
    {
        var repoRespuestas = new Mock<IRepositorioRespuestasTrivia>();
        SetupRepoRespuestasDefault(repoRespuestas);
        repoRespuestas.Setup(r => r.ContarRespuestasDeJugadorEnEtapaAsync(
                sesion.Id, EtapaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        return new EnviarRespuestaTriviaManejador(
            BuildUsuario(),
            BuildRepo(sesion),
            BuildClienteTrivia(),
            repoRespuestas.Object,
            notificador.Object,
            Mock.Of<IServicioFinalizacionSesion>());
    }
}
