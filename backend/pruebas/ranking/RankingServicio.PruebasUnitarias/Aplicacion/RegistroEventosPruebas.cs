using FluentAssertions;
using RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;
using RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Aplicacion;

public sealed class RegistroEventosPruebas
{
    // ─── ProcesarEquipoCreado ────────────────────────────────────────────

    [Fact]
    public async Task EquipoCreado_sinRankingPrevio_creaRankingYRegistraEquipo()
    {
        var repo = new FakeRepo();
        var eventos = new FakeEventos();
        var comando = new ProcesarEquipoCreadoComando(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var manejador = CrearManejadorEquipo(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking.Should().NotBeNull();
        repo.Ranking!.Equipos.Should().ContainSingle(e => e.EquipoId == comando.EquipoId);
        eventos.Procesados.Should().ContainKey(comando.EventoId);
    }

    [Fact]
    public async Task EquipoCreado_conRankingExistente_registraEquipoEnElExistente()
    {
        var sesionId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        var repo = new FakeRepo(ranking);
        var eventos = new FakeEventos();
        var equipoId = Guid.NewGuid();
        var comando = new ProcesarEquipoCreadoComando(Guid.NewGuid(), sesionId, equipoId);
        var manejador = CrearManejadorEquipo(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking!.Id.Should().Be(ranking.Id);
        repo.Ranking.Equipos.Should().ContainSingle(e => e.EquipoId == equipoId);
    }

    [Fact]
    public async Task EquipoCreado_eventoDuplicado_esIdempotente()
    {
        var repo = new FakeRepo();
        var eventos = new FakeEventos();
        var comando = new ProcesarEquipoCreadoComando(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var manejador = CrearManejadorEquipo(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);
        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking!.Equipos.Should().ContainSingle();
    }

    // ─── ProcesarParticipanteUnido ───────────────────────────────────────

    [Fact]
    public async Task ParticipanteUnido_sinRankingPrevio_creaRankingYRegistraParticipante()
    {
        var repo = new FakeRepo();
        var eventos = new FakeEventos();
        var participanteId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();
        var comando = new ProcesarParticipanteUnidoComando(
            Guid.NewGuid(), Guid.NewGuid(), participanteId, identidadId, null);
        var manejador = CrearManejadorParticipante(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking.Should().NotBeNull();
        repo.Ranking!.Participantes.Should().ContainSingle(
            p => p.ParticipanteSesionId == participanteId &&
                 p.ParticipanteIdentidadId == identidadId);
        eventos.Procesados.Should().ContainKey(comando.EventoId);
    }

    [Fact]
    public async Task ParticipanteUnido_conRankingExistente_registraParticipanteEnElExistente()
    {
        var sesionId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        var repo = new FakeRepo(ranking);
        var eventos = new FakeEventos();
        var participanteId = Guid.NewGuid();
        var comando = new ProcesarParticipanteUnidoComando(
            Guid.NewGuid(), sesionId, participanteId, Guid.NewGuid(), null);
        var manejador = CrearManejadorParticipante(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking!.Id.Should().Be(ranking.Id);
        repo.Ranking.Participantes.Should().ContainSingle(p => p.ParticipanteSesionId == participanteId);
    }

    [Fact]
    public async Task ParticipanteUnido_eventoDuplicado_esIdempotente()
    {
        var repo = new FakeRepo();
        var eventos = new FakeEventos();
        var comando = new ProcesarParticipanteUnidoComando(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        var manejador = CrearManejadorParticipante(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);
        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking!.Participantes.Should().ContainSingle();
    }

    [Fact]
    public async Task ParticipanteUnido_conEquipo_registraParticipanteConEquipo()
    {
        var repo = new FakeRepo();
        var eventos = new FakeEventos();
        var equipoId = Guid.NewGuid();
        var comando = new ProcesarParticipanteUnidoComando(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), equipoId);
        var manejador = CrearManejadorParticipante(repo, eventos);

        await manejador.Handle(comando, CancellationToken.None);

        repo.Ranking!.Participantes.Single().EquipoId.Should().Be(equipoId);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static ProcesarEquipoCreadoManejador CrearManejadorEquipo(
        FakeRepo repo, FakeEventos eventos)
        => new(repo, eventos, new FakeUnidad(), new FakeReloj());

    private static ProcesarParticipanteUnidoManejador CrearManejadorParticipante(
        FakeRepo repo, FakeEventos eventos)
        => new(repo, eventos, new FakeUnidad(), new FakeReloj());

    // ─── Fakes ───────────────────────────────────────────────────────────

    private sealed class FakeRepo : IRepositorioRanking
    {
        public Ranking? Ranking { get; private set; }

        public FakeRepo() { }
        public FakeRepo(Ranking ranking) { Ranking = ranking; }

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
        public Dictionary<Guid, string> Procesados { get; } = new();

        public Task<bool> ExisteAsync(Guid eventoId, string tipo, CancellationToken cancelacion)
            => Task.FromResult(Procesados.ContainsKey(eventoId));

        public Task RegistrarAsync(Guid eventoId, string tipo, DateTime ahora, CancellationToken cancelacion)
        {
            Procesados[eventoId] = tipo;
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

    private sealed class FakeReloj : IProveedorFechaHora
    {
        public DateTime ObtenerFechaHoraUtc()
            => new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
    }
}
