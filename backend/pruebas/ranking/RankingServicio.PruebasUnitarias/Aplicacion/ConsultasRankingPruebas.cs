using FluentAssertions;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Aplicacion;

public sealed class ConsultasRankingPruebas
{
    // ─── ObtenerRankingParticipantesSesion ───────────────────────────────

    [Fact]
    public async Task Participantes_sinRankingParaSesion_retornaListaVacia()
    {
        var repo = new FakeRepo();
        var manejador = new ObtenerRankingParticipantesSesionManejador(repo, new FakeClienteIdentidad());

        var resultado = await manejador.Handle(
            new ObtenerRankingParticipantesSesionConsulta(Guid.NewGuid()),
            CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Participantes_conParticipantes_retornaOrdenadosPorPuntaje()
    {
        var sesionId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), null, 10);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), null, 30);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), null, 20);

        var repo = new FakeRepo(sesionId, ranking);
        var manejador = new ObtenerRankingParticipantesSesionManejador(repo, new FakeClienteIdentidad());

        var resultado = await manejador.Handle(
            new ObtenerRankingParticipantesSesionConsulta(sesionId),
            CancellationToken.None);

        resultado.Should().HaveCount(3);
        resultado[0].Posicion.Should().Be(1);
        resultado[0].Puntaje.Should().Be(30);
        resultado[1].Puntaje.Should().Be(20);
        resultado[2].Puntaje.Should().Be(10);
    }

    [Fact]
    public async Task Participantes_sinDatosIdentidad_usaGuidComoAlias()
    {
        var sesionId = Guid.NewGuid();
        var idParticipante = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), idParticipante, null, 5);

        var repo = new FakeRepo(sesionId, ranking);
        // cliente devuelve diccionario vacío → fallback al GUID
        var manejador = new ObtenerRankingParticipantesSesionManejador(repo, new FakeClienteIdentidad());

        var resultado = await manejador.Handle(
            new ObtenerRankingParticipantesSesionConsulta(sesionId),
            CancellationToken.None);

        resultado.Single().Alias.Should().Be(idParticipante.ToString());
    }

    [Fact]
    public async Task Participantes_conAlias_usaAliasDeIdentidad()
    {
        var sesionId = Guid.NewGuid();
        var idParticipante = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), idParticipante, null, 5);

        var repo = new FakeRepo(sesionId, ranking);
        var clienteConAlias = new FakeClienteIdentidad(new Dictionary<Guid, ParticipanteIdentidadResumenDto>
        {
            [idParticipante] = new() { Id = idParticipante, Alias = "DrRanking" }
        });
        var manejador = new ObtenerRankingParticipantesSesionManejador(repo, clienteConAlias);

        var resultado = await manejador.Handle(
            new ObtenerRankingParticipantesSesionConsulta(sesionId),
            CancellationToken.None);

        resultado.Single().Alias.Should().Be("DrRanking");
    }

    // ─── ObtenerRankingEquiposSesion ─────────────────────────────────────

    [Fact]
    public async Task Equipos_sinRankingParaSesion_retornaListaVacia()
    {
        var manejador = new ObtenerRankingEquiposSesionManejador(
            new FakeRepo(), new FakeClienteIdentidad(), new FakeClienteSesiones());

        var resultado = await manejador.Handle(
            new ObtenerRankingEquiposSesionConsulta(Guid.NewGuid()),
            CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Equipos_rankingSinEquipos_retornaListaVacia()
    {
        var sesionId = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        // solo participantes individuales, sin equipos
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), null, 5);

        var manejador = new ObtenerRankingEquiposSesionManejador(
            new FakeRepo(sesionId, ranking), new FakeClienteIdentidad(), new FakeClienteSesiones());

        var resultado = await manejador.Handle(
            new ObtenerRankingEquiposSesionConsulta(sesionId),
            CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Equipos_conEquipos_retornaOrdenadosPorPuntaje()
    {
        var sesionId = Guid.NewGuid();
        var equipoA = Guid.NewGuid();
        var equipoB = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoA, 10);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), equipoB, 30);

        var manejador = new ObtenerRankingEquiposSesionManejador(
            new FakeRepo(sesionId, ranking), new FakeClienteIdentidad(), new FakeClienteSesiones());

        var resultado = await manejador.Handle(
            new ObtenerRankingEquiposSesionConsulta(sesionId),
            CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].Posicion.Should().Be(1);
        resultado[0].Puntaje.Should().Be(30);
        resultado[1].Puntaje.Should().Be(10);
    }

    // ─── ObtenerRankingGlobal ────────────────────────────────────────────

    [Fact]
    public async Task Global_topCero_usa50ComoDefault()
    {
        int topRecibido = 0;
        var consultas = new FakeConsultasRanking(top => { topRecibido = top; return new List<RankingGlobalProyeccion>(); });
        var manejador = new ObtenerRankingGlobalManejador(consultas, new FakeClienteIdentidad());

        await manejador.Handle(new ObtenerRankingGlobalConsulta(0), CancellationToken.None);

        topRecibido.Should().Be(50);
    }

    [Fact]
    public async Task Global_topNegativo_usa50ComoDefault()
    {
        int topRecibido = 0;
        var consultas = new FakeConsultasRanking(top => { topRecibido = top; return new List<RankingGlobalProyeccion>(); });
        var manejador = new ObtenerRankingGlobalManejador(consultas, new FakeClienteIdentidad());

        await manejador.Handle(new ObtenerRankingGlobalConsulta(-5), CancellationToken.None);

        topRecibido.Should().Be(50);
    }

    [Fact]
    public async Task Global_conProyecciones_retornaDtosConPosicion()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var proyecciones = new List<RankingGlobalProyeccion>
        {
            new(id1, 100),
            new(id2, 50)
        };
        var consultas = new FakeConsultasRanking(_ => proyecciones);
        var manejador = new ObtenerRankingGlobalManejador(consultas, new FakeClienteIdentidad());

        var resultado = await manejador.Handle(
            new ObtenerRankingGlobalConsulta(10), CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].Posicion.Should().Be(1);
        resultado[0].Puntaje.Should().Be(100);
        resultado[1].Posicion.Should().Be(2);
        resultado[1].Puntaje.Should().Be(50);
    }

    [Fact]
    public async Task Participantes_conNombreYApellido_usaNombreApellido()
    {
        var sesionId = Guid.NewGuid();
        var idParticipante = Guid.NewGuid();
        var ranking = Ranking.Crear(sesionId);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), idParticipante, null, 5);

        var repo = new FakeRepo(sesionId, ranking);
        var clienteConNombre = new FakeClienteIdentidad(new Dictionary<Guid, ParticipanteIdentidadResumenDto>
        {
            [idParticipante] = new() { Id = idParticipante, Nombre = "Juan", Apellido = "Pérez" }
        });
        var manejador = new ObtenerRankingParticipantesSesionManejador(repo, clienteConNombre);

        var resultado = await manejador.Handle(
            new ObtenerRankingParticipantesSesionConsulta(sesionId),
            CancellationToken.None);

        resultado.Single().Alias.Should().Be("Juan Pérez");
    }

    // ─── Fakes ───────────────────────────────────────────────────────────

    private sealed class FakeRepo : IRepositorioRanking
    {
        private readonly Guid? _sesionId;
        private readonly Ranking? _ranking;

        public FakeRepo() { }
        public FakeRepo(Guid sesionId, Ranking ranking) { _sesionId = sesionId; _ranking = ranking; }

        public Task<Ranking?> ObtenerPorSesionAsync(Guid sesionId, CancellationToken cancelacion)
            => Task.FromResult(_sesionId == sesionId ? _ranking : null);

        public Task AgregarAsync(Ranking ranking, CancellationToken cancelacion) => Task.CompletedTask;
        public Task ActualizarAsync(Ranking ranking, CancellationToken cancelacion) => Task.CompletedTask;
    }

    private sealed class FakeClienteIdentidad : IClienteIdentidadParticipantes
    {
        private readonly IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto> _datos;

        public FakeClienteIdentidad()
            => _datos = new Dictionary<Guid, ParticipanteIdentidadResumenDto>();

        public FakeClienteIdentidad(Dictionary<Guid, ParticipanteIdentidadResumenDto> datos)
            => _datos = datos;

        public Task<IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>> ObtenerParticipantesPorIdsAsync(
            IEnumerable<Guid> ids, CancellationToken cancelacion)
            => Task.FromResult(_datos);
    }

    private sealed class FakeClienteSesiones : IClienteSesionesRanking
    {
        public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresEquiposAsync(
            Guid sesionId, CancellationToken cancelacion)
            => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
    }

    private sealed class FakeConsultasRanking : IConsultasRanking
    {
        private readonly Func<int, IReadOnlyList<RankingGlobalProyeccion>> _callback;

        public FakeConsultasRanking(Func<int, IReadOnlyList<RankingGlobalProyeccion>> callback)
            => _callback = callback;

        public Task<IReadOnlyList<RankingGlobalProyeccion>> ObtenerRankingGlobalAsync(
            int top, CancellationToken cancelacion)
            => Task.FromResult(_callback(top));
    }
}
