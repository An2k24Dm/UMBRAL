using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RankingServicio.Aplicacion.Comandos.ProcesarPenalizacion;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Aplicacion;

// HU52 — Orquestación de ProcesarPenalizacionManejador: aplica la penalización
// al objetivo correcto, persiste, registra el evento procesado, publica el
// resultado y notifica SignalR. Idempotente por EventoId.
public sealed class ProcesamientoPenalizacionPruebas
{
    private const string Participante = "Participante";
    private const string Equipo = "Equipo";

    [Fact]
    public async Task Penalizacion_individual_descuentaEmiteResultadoYNotifica()
    {
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 3);
        var entorno = new Entorno(ranking);
        var comando = ComandoParticipante(sesionId, participanteSesionId, identidadId, puntos: 5);

        await entorno.Manejador().Handle(comando, CancellationToken.None);

        var participante = entorno.Repositorio.Ranking!.Participantes.Single();
        participante.Puntaje.Valor.Should().Be(-2);
        participante.PuntosPenalizados.Should().Be(5);

        entorno.Publicador.Penalizaciones.Should().ContainSingle();
        var resultado = entorno.Publicador.Penalizaciones.Single();
        resultado.EventoIdOrigen.Should().Be(comando.EventoId);
        resultado.TipoObjetivo.Should().Be(Participante);
        resultado.PuntosPenalizados.Should().Be(5);
        resultado.PuntosPenalizadosAcumulados.Should().Be(5);
        resultado.PuntajeTotalParticipante.Should().Be(-2);
        resultado.PuntajeTotalEquipo.Should().BeNull();

        entorno.Notificador.Participantes.Should().Contain(sesionId);
        entorno.Notificador.Equipos.Should().BeEmpty();
        entorno.Notificador.Penalizaciones.Should().ContainSingle();
        entorno.Eventos.FueRegistrado(comando.EventoId).Should().BeTrue();
    }

    [Fact]
    public async Task Penalizacion_grupal_afectaEquipoYNotificaEquipos()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoId, 50);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoId, 30);
        var entorno = new Entorno(ranking);
        var comando = ComandoEquipo(sesionId, equipoId, puntos: 20);

        await entorno.Manejador().Handle(comando, CancellationToken.None);

        var equipo = entorno.Repositorio.Ranking!.Equipos.Single(e => e.EquipoId == equipoId);
        equipo.Puntaje.Valor.Should().Be(60);
        equipo.PuntosPenalizados.Should().Be(20);

        var resultado = entorno.Publicador.Penalizaciones.Single();
        resultado.TipoObjetivo.Should().Be(Equipo);
        resultado.PuntajeTotalEquipo.Should().Be(60);
        resultado.PuntosPenalizadosAcumulados.Should().Be(20);
        resultado.PuntajeTotalParticipante.Should().BeNull();

        entorno.Notificador.Equipos.Should().Contain(sesionId);
        entorno.Notificador.Penalizaciones.Should().ContainSingle();
    }

    [Fact]
    public async Task Penalizacion_puntajeIndividualPuedeQuedarNegativo()
    {
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarParticipante(participanteSesionId, identidadId, null);
        var entorno = new Entorno(ranking);

        await entorno.Manejador().Handle(
            ComandoParticipante(sesionId, participanteSesionId, identidadId, puntos: 5),
            CancellationToken.None);

        entorno.Repositorio.Ranking!.Participantes.Single().Puntaje.Valor.Should().Be(-5);
    }

    [Fact]
    public async Task Penalizacion_eventoDuplicado_noSeAplicaDosVeces()
    {
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 10);
        var entorno = new Entorno(ranking);
        var comando = ComandoParticipante(sesionId, participanteSesionId, identidadId, puntos: 5);

        await entorno.Manejador().Handle(comando, CancellationToken.None);
        await entorno.Manejador().Handle(comando, CancellationToken.None);

        var participante = entorno.Repositorio.Ranking!.Participantes.Single();
        participante.PuntosPenalizados.Should().Be(5);
        participante.Puntaje.Valor.Should().Be(5);
        entorno.Publicador.Penalizaciones.Should().ContainSingle();
        entorno.Notificador.Penalizaciones.Should().ContainSingle();
    }

    [Fact]
    public async Task Penalizacion_sinRanking_creaRankingYAplica()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var entorno = new Entorno(rankingInicial: null);

        await entorno.Manejador().Handle(
            ComandoEquipo(sesionId, equipoId, puntos: 10), CancellationToken.None);

        var equipo = entorno.Repositorio.Ranking!.Equipos.Single();
        equipo.PuntosPenalizados.Should().Be(10);
        equipo.Puntaje.Valor.Should().Be(-10);
    }

    private static ProcesarPenalizacionComando ComandoParticipante(
        Guid sesionId, Guid participanteSesionId, Guid identidadId, int puntos)
        => new(
            Guid.NewGuid(), Guid.NewGuid(), sesionId, Participante,
            participanteSesionId, identidadId, null,
            puntos, "Incumplimiento", Guid.NewGuid(), DateTime.UtcNow);

    private static ProcesarPenalizacionComando ComandoEquipo(
        Guid sesionId, Guid equipoId, int puntos)
        => new(
            Guid.NewGuid(), Guid.NewGuid(), sesionId, Equipo,
            null, null, equipoId,
            puntos, "Incumplimiento", Guid.NewGuid(), DateTime.UtcNow);

    private sealed class Entorno
    {
        public FakeRepositorioRanking Repositorio { get; }
        public FakeEventos Eventos { get; } = new();
        public FakeNotificador Notificador { get; } = new();
        public FakePublicador Publicador { get; } = new();

        public Entorno(Ranking? rankingInicial)
        {
            Repositorio = new FakeRepositorioRanking(rankingInicial);
        }

        public ProcesarPenalizacionManejador Manejador()
            => new(
                Repositorio, Eventos, new FakeUnidad(), Notificador, Publicador,
                new FakeReloj(), NullLogger<ProcesarPenalizacionManejador>.Instance);
    }

    private sealed class FakeRepositorioRanking : IRepositorioRanking
    {
        public Ranking? Ranking { get; private set; }

        public FakeRepositorioRanking(Ranking? ranking) => Ranking = ranking;

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

    private sealed class FakeEventos : IRepositorioEventosProcesados
    {
        private readonly HashSet<(Guid, string)> _procesados = new();

        public bool FueRegistrado(Guid eventoId) => _procesados.Any(p => p.Item1 == eventoId);

        public Task<bool> ExisteAsync(Guid eventoId, string tipoEvento, CancellationToken cancelacion)
            => Task.FromResult(_procesados.Contains((eventoId, tipoEvento)));

        public Task RegistrarAsync(Guid eventoId, string tipoEvento, DateTime ahora, CancellationToken cancelacion)
        {
            _procesados.Add((eventoId, tipoEvento));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnidad : IUnidadTrabajoRanking
    {
        public Task GuardarCambiosAsync(CancellationToken cancelacion) => Task.CompletedTask;

        public Task EjecutarEnTransaccionAsync(
            Func<CancellationToken, Task> operacion, CancellationToken cancelacion)
            => operacion(cancelacion);
    }

    private sealed class FakeNotificador : INotificadorRankingTiempoReal
    {
        public List<Guid> Participantes { get; } = new();
        public List<Guid> Equipos { get; } = new();
        public List<PenalizacionAplicadaNotificacionDto> Penalizaciones { get; } = new();

        public Task NotificarPuntajeCalculadoAsync(PuntajeCalculadoDto puntaje, CancellationToken cancelacion)
            => Task.CompletedTask;

        public Task NotificarRankingParticipantesActualizadoAsync(Guid sesionId, CancellationToken cancelacion)
        {
            Participantes.Add(sesionId);
            return Task.CompletedTask;
        }

        public Task NotificarRankingEquiposActualizadoAsync(Guid sesionId, CancellationToken cancelacion)
        {
            Equipos.Add(sesionId);
            return Task.CompletedTask;
        }

        public Task NotificarPenalizacionAplicadaAsync(
            PenalizacionAplicadaNotificacionDto penalizacion, CancellationToken cancelacion)
        {
            Penalizaciones.Add(penalizacion);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePublicador : IPublicadorResultadosPuntaje
    {
        public List<PenalizacionProcesadaDto> Penalizaciones { get; } = new();

        public Task PublicarPuntajeActualizadoAsync(PuntajeCalculadoDto puntaje, CancellationToken cancelacion)
            => Task.CompletedTask;

        public Task PublicarPenalizacionProcesadaAsync(PenalizacionProcesadaDto penalizacion, CancellationToken cancelacion)
        {
            Penalizaciones.Add(penalizacion);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReloj : IProveedorFechaHora
    {
        public DateTime ObtenerFechaHoraUtc() => new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    }
}
