using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using PartidasServicio.Aplicacion.Estrategias;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Aplicacion.Validaciones;
using PartidasServicio.Commons.Dtos;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.PruebasUnitarias.CasosDeUso;

public class EnviarRespuestaTriviaManejadorPruebas
{
    // ─── dependencias mockeadas ────────────────────────────────────────────────
    private readonly Mock<IRepositorioRespuestas> _repositorio = new();
    private readonly Mock<IUnidadTrabajoPartidas> _unidad = new();
    private readonly Mock<IUsuarioActual> _usuario = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private readonly Mock<IClienteJuegos> _clienteJuegos = new();
    private readonly Mock<IClienteSesiones> _clienteSesiones = new();
    private readonly Mock<IConsultasPartidas> _consultas = new();
    private readonly Mock<INotificadorPartidasTiempoReal> _notificador = new();
    private readonly Mock<IRegistroLogsAplicacion> _logs = new();

    // ─── constantes ────────────────────────────────────────────────────────────
    private static readonly Guid ParticipanteId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SesionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid MisionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid EtapaId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid TriviaId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PreguntaId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid OpcionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime AhoraUtc = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    public EnviarRespuestaTriviaManejadorPruebas()
    {
        // Defaults: sesión activa, participante inscrito, pregunta no respondida
        _usuario.Setup(u => u.ObtenerId()).Returns(ParticipanteId);
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
        _unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repositorio.Setup(r => r.AgregarAsync(It.IsAny<RespuestaTrivia>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio.Setup(r => r.YaRespondioEquipoAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto
            {
                Estado = "Activa",
                ParticipanteInscrito = true,
                EquipoId = null
            });
        _clienteJuegos.Setup(c => c.VerificarRespuestaAsync(TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificacionRespuestaDto
            {
                EsCorrecta = true,
                PuntajeBase = 100,
                TiempoLimiteMs = 10_000
            });
        _consultas.Setup(c => c.ObtenerRankingAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RankingEntradaDto>());
        _notificador.Setup(n => n.NotificarPuntajeActualizadoAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<RankingEntradaDto>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private EnviarRespuestaTriviaManejador CrearManejador()
    {
        var validador = new ValidadorEnviarRespuestaTrivia();
        var eslabonEstado = new EslabonEstadoSesion(_clienteSesiones.Object);
        var eslabonParticipante = new EslabonParticipanteEnSesion();
        var eslabonConcurrencia = new EslabonConcurrencia(_repositorio.Object);
        var calculadora = new CalculadoraPuntajePorTiempo();

        return new EnviarRespuestaTriviaManejador(
            validador, _repositorio.Object, _unidad.Object,
            _usuario.Object, _reloj.Object, _clienteJuegos.Object,
            _consultas.Object, _notificador.Object, calculadora, _logs.Object,
            eslabonEstado, eslabonParticipante, eslabonConcurrencia);
    }

    private EnviarRespuestaTriviaComando ComandoValido(long tiempoMs = 0) => new(
        SesionId, MisionId, EtapaId, TriviaId,
        new EnviarRespuestaTriviaDto
        {
            PreguntaId = PreguntaId,
            OpcionSeleccionadaId = OpcionId,
            TiempoTardadoMs = tiempoMs
        });

    // ─── camino feliz ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RespuestaCorrecta_RetornaEsCorrectaTrue()
    {
        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.EsCorrecta.Should().BeTrue();
        resultado.YaRespondida.Should().BeFalse();
        resultado.PuntosGanados.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_RespuestaCorrecta_PersisteLaRespuesta()
    {
        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarAsync(It.IsAny<RespuestaTrivia>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RespuestaCorrecta_NotificaRankingPorSignalR()
    {
        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _notificador.Verify(
            n => n.NotificarPuntajeActualizadoAsync(SesionId, It.IsAny<IReadOnlyList<RankingEntradaDto>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RespuestaIncorrecta_RetornaCeroPuntos()
    {
        _clienteJuegos.Setup(c => c.VerificarRespuestaAsync(TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificacionRespuestaDto { EsCorrecta = false, PuntajeBase = 100, TiempoLimiteMs = 10_000 });

        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.EsCorrecta.Should().BeFalse();
        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RespuestaIncorrecta_NoNotificaSignalR()
    {
        _clienteJuegos.Setup(c => c.VerificarRespuestaAsync(TriviaId, PreguntaId, OpcionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VerificacionRespuestaDto { EsCorrecta = false, PuntajeBase = 100, TiempoLimiteMs = 10_000 });

        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _notificador.Verify(
            n => n.NotificarPuntajeActualizadoAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<RankingEntradaDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Strategy: puntaje por tiempo ─────────────────────────────────────────

    [Fact]
    public async Task Handle_RespuestaInstantanea_RetornaPuntajeMaximo()
    {
        var resultado = await CrearManejador().Handle(ComandoValido(tiempoMs: 0), CancellationToken.None);

        resultado.PuntosGanados.Should().Be(100);
    }

    [Fact]
    public async Task Handle_RespuestaEnLimite_RetornaMitadDelPuntaje()
    {
        var resultado = await CrearManejador().Handle(ComandoValido(tiempoMs: 10_000), CancellationToken.None);

        resultado.PuntosGanados.Should().Be(50);
    }

    // ─── State: sesión no activa ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_SesionNoActiva_LanzaSesionNoActivaExcepcion()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto { Estado = "Programada", ParticipanteInscrito = true });

        var accion = async () => await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoActivaExcepcion>();
    }

    [Fact]
    public async Task Handle_SesionNoActiva_NoConsultaJuegosServicio()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto { Estado = "Finalizada", ParticipanteInscrito = true });

        try { await CrearManejador().Handle(ComandoValido(), CancellationToken.None); } catch { }

        _clienteJuegos.Verify(
            c => c.VerificarRespuestaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Chain: participante no inscrito ──────────────────────────────────────

    [Fact]
    public async Task Handle_ParticipanteNoInscrito_LanzaParticipanteNoEnSesionExcepcion()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto { Estado = "Activa", ParticipanteInscrito = false });

        var accion = async () => await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        await accion.Should().ThrowAsync<ParticipanteNoEnSesionExcepcion>();
    }

    // ─── Chain: ya respondida ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PreguntaYaRespondidaIndividual_RetornaYaRespondidaTrue()
    {
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                SesionId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.YaRespondida.Should().BeTrue();
        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PreguntaYaRespondidaEquipo_RetornaYaRespondidaTrue()
    {
        var equipoId = Guid.NewGuid();
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto { Estado = "Activa", ParticipanteInscrito = true, EquipoId = equipoId });
        _repositorio.Setup(r => r.YaRespondioEquipoAsync(SesionId, PreguntaId, equipoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.YaRespondida.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PreguntaYaRespondida_NoLlamaJuegosServicio()
    {
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                SesionId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _clienteJuegos.Verify(
            c => c.VerificarRespuestaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── concurrencia: RespuestaDuplicadaExcepcion ────────────────────────────

    [Fact]
    public async Task Handle_RespuestaDuplicadaExcepcion_RetornaYaRespondidaTrue()
    {
        _repositorio.Setup(r => r.AgregarAsync(It.IsAny<RespuestaTrivia>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RespuestaDuplicadaExcepcion());

        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.YaRespondida.Should().BeTrue();
        resultado.PuntosGanados.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RespuestaDuplicadaExcepcion_NoNotificaSignalR()
    {
        _repositorio.Setup(r => r.AgregarAsync(It.IsAny<RespuestaTrivia>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RespuestaDuplicadaExcepcion());

        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _notificador.Verify(
            n => n.NotificarPuntajeActualizadoAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<RankingEntradaDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── validación de entrada ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SesionIdVacio_LanzaExcepcionValidacion()
    {
        var comando = new EnviarRespuestaTriviaComando(
            Guid.Empty, MisionId, EtapaId, TriviaId,
            new EnviarRespuestaTriviaDto { PreguntaId = PreguntaId, OpcionSeleccionadaId = OpcionId, TiempoTardadoMs = 0 });

        var accion = async () => await CrearManejador().Handle(comando, CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task Handle_ComandoInvalido_NoLlamaNingunaDepenenciaExterna()
    {
        var comando = new EnviarRespuestaTriviaComando(
            Guid.Empty, Guid.Empty, Guid.Empty, TriviaId,
            new EnviarRespuestaTriviaDto { PreguntaId = Guid.Empty, OpcionSeleccionadaId = Guid.Empty, TiempoTardadoMs = -1 });

        try { await CrearManejador().Handle(comando, CancellationToken.None); } catch { }

        _clienteSesiones.Verify(c => c.ObtenerInfoPartidaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _clienteJuegos.Verify(c => c.VerificarRespuestaAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositorio.Verify(r => r.AgregarAsync(It.IsAny<RespuestaTrivia>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
