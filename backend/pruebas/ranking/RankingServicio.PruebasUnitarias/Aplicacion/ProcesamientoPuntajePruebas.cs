using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RankingServicio.Aplicacion.Comandos.ProcesarEvidenciaTesoro;
using RankingServicio.Aplicacion.Comandos.ProcesarRespuestaTrivia;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Abstract;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.Estrategias;
using RankingServicio.Dominio.ObjetosValor;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Aplicacion;

public sealed class ProcesamientoPuntajePruebas
{
    [Fact]
    public async Task Trivia_correcta_actualizaRankingYEmitePuntajeCalculado()
    {
        var entorno = new EntornoHandler();
        var comando = CrearComandoTrivia(esCorrecta: true, puntajeBase: 5);
        var manejador = entorno.CrearManejadorTrivia();

        await manejador.Handle(comando, CancellationToken.None);

        var participante = entorno.Repositorio.Ranking!.Participantes.Single();
        participante.Puntaje.Valor.Should().Be(5);
        entorno.Notificador.Puntajes.Should().ContainSingle();
        var puntaje = entorno.Notificador.Puntajes.Single();
        puntaje.EventoIdOrigen.Should().Be(comando.EventoId);
        puntaje.PuntajeGanado.Should().Be(5);
        puntaje.PuntajeTotalParticipante.Should().Be(5);
        entorno.Publicador.Puntajes.Should().ContainSingle(p => p.EventoIdOrigen == comando.EventoId);
    }

    [Fact]
    public async Task Trivia_incorrecta_emitePuntajeCalculadoConCero()
    {
        var entorno = new EntornoHandler();
        var comando = CrearComandoTrivia(esCorrecta: false, puntajeBase: 5);
        var manejador = entorno.CrearManejadorTrivia();

        await manejador.Handle(comando, CancellationToken.None);

        entorno.Repositorio.Ranking!.Participantes.Single().Puntaje.Valor.Should().Be(0);
        var puntaje = entorno.Notificador.Puntajes.Single();
        puntaje.EventoIdOrigen.Should().Be(comando.EventoId);
        puntaje.PuntajeGanado.Should().Be(0);
    }

    [Fact]
    public async Task Tesoro_valido_actualizaRankingYEmitePuntajeCalculado()
    {
        var entorno = new EntornoHandler();
        var comando = CrearComandoTesoro(esValida: true, puntajeBase: 50);
        var manejador = entorno.CrearManejadorTesoro();

        await manejador.Handle(comando, CancellationToken.None);

        entorno.Repositorio.Ranking!.Participantes.Single().Puntaje.Valor.Should().Be(50);
        var puntaje = entorno.Notificador.Puntajes.Single();
        puntaje.EventoIdOrigen.Should().Be(comando.EventoId);
        puntaje.PuntajeGanado.Should().Be(50);
        puntaje.PuntajeTotalParticipante.Should().Be(50);
    }

    [Fact]
    public async Task Tesoro_invalido_emitePuntajeCalculadoConCero()
    {
        var entorno = new EntornoHandler();
        var comando = CrearComandoTesoro(esValida: false, puntajeBase: 50);
        var manejador = entorno.CrearManejadorTesoro();

        await manejador.Handle(comando, CancellationToken.None);

        entorno.Repositorio.Ranking!.Participantes.Single().Puntaje.Valor.Should().Be(0);
        var puntaje = entorno.Notificador.Puntajes.Single();
        puntaje.EventoIdOrigen.Should().Be(comando.EventoId);
        puntaje.PuntajeGanado.Should().Be(0);
    }

    [Fact]
    public async Task EventoDuplicado_noSumaDosVecesNiReemite()
    {
        var entorno = new EntornoHandler();
        var comando = CrearComandoTrivia(esCorrecta: true, puntajeBase: 5);
        var manejador = entorno.CrearManejadorTrivia();

        await manejador.Handle(comando, CancellationToken.None);
        await manejador.Handle(comando, CancellationToken.None);

        entorno.Repositorio.Ranking!.Participantes.Single().Puntaje.Valor.Should().Be(5);
        entorno.Notificador.Puntajes.Should().ContainSingle();
        entorno.Publicador.Puntajes.Should().ContainSingle();
    }

    private static ProcesarRespuestaTriviaComando CrearComandoTrivia(
        bool esCorrecta,
        int puntajeBase)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EquipoId: null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            esCorrecta,
            puntajeBase,
            TiempoTardadoMs: 0,
            TiempoLimiteMs: 10_000);

    private static ProcesarEvidenciaTesoroComando CrearComandoTesoro(
        bool esValida,
        int puntajeBase)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EquipoId: null,
            Guid.NewGuid(),
            esValida,
            puntajeBase);

    private sealed class EntornoHandler
    {
        public FakeRepositorioRanking Repositorio { get; } = new();
        public FakeRepositorioEventosProcesados Eventos { get; } = new();
        public FakeNotificadorRanking Notificador { get; } = new();
        public FakePublicadorResultados Publicador { get; } = new();

        public ProcesarRespuestaTriviaManejador CrearManejadorTrivia()
            => new(
                Repositorio,
                Eventos,
                new FakeUnidadTrabajo(),
                Notificador,
                Publicador,
                new FakeReloj(),
                new EstrategiaPuntajeTriviaPorTiempo(),
                NullLogger<ProcesarRespuestaTriviaManejador>.Instance);

        public ProcesarEvidenciaTesoroManejador CrearManejadorTesoro()
            => new(
                Repositorio,
                Eventos,
                new FakeUnidadTrabajo(),
                Notificador,
                Publicador,
                new FakeReloj(),
                new EstrategiaPuntajeBusquedaTesoro(),
                NullLogger<ProcesarEvidenciaTesoroManejador>.Instance);
    }

    private sealed class FakeRepositorioRanking : IRepositorioRanking
    {
        public Ranking? Ranking { get; private set; }

        public Task<Ranking?> ObtenerPorSesionAsync(Guid sesionId, CancellationToken cancelacion)
            => Task.FromResult(Ranking?.SesionId == sesionId ? Ranking : null);

        public Task AgregarAsync(Ranking ranking, CancellationToken cancelacion)
        {
            Ranking = ranking;
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Ranking ranking, CancellationToken cancelacion)
        {
            Ranking = ranking;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepositorioEventosProcesados : IRepositorioEventosProcesados
    {
        private readonly HashSet<(Guid Id, string Tipo)> _procesados = new();

        public Task<bool> ExisteAsync(Guid eventoId, string tipoEvento, CancellationToken cancelacion)
            => Task.FromResult(_procesados.Contains((eventoId, tipoEvento)));

        public Task RegistrarAsync(
            Guid eventoId,
            string tipoEvento,
            DateTime ahora,
            CancellationToken cancelacion)
        {
            _procesados.Add((eventoId, tipoEvento));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnidadTrabajo : IUnidadTrabajoRanking
    {
        public Task GuardarCambiosAsync(CancellationToken cancelacion)
            => Task.CompletedTask;

        public Task EjecutarEnTransaccionAsync(
            Func<CancellationToken, Task> operacion,
            CancellationToken cancelacion)
            => operacion(cancelacion);
    }

    private sealed class FakeNotificadorRanking : INotificadorRankingTiempoReal
    {
        public List<PuntajeCalculadoDto> Puntajes { get; } = new();

        public Task NotificarPuntajeCalculadoAsync(
            PuntajeCalculadoDto puntaje,
            CancellationToken cancelacion)
        {
            Puntajes.Add(puntaje);
            return Task.CompletedTask;
        }

        public Task NotificarRankingParticipantesActualizadoAsync(
            Guid sesionId,
            CancellationToken cancelacion)
            => Task.CompletedTask;

        public Task NotificarRankingEquiposActualizadoAsync(
            Guid sesionId,
            CancellationToken cancelacion)
            => Task.CompletedTask;
    }

    private sealed class FakePublicadorResultados : IPublicadorResultadosPuntaje
    {
        public List<PuntajeCalculadoDto> Puntajes { get; } = new();

        public Task PublicarPuntajeActualizadoAsync(
            PuntajeCalculadoDto puntaje,
            CancellationToken cancelacion)
        {
            Puntajes.Add(puntaje);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReloj : IProveedorFechaHora
    {
        public DateTime ObtenerFechaHoraUtc()
            => new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
    }
}
