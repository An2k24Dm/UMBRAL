using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;

namespace SesionesServicio.PruebasUnitarias.Persistencia;

public sealed class RepositorioPenalizacionesAplicadasPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Agregar_yListarPorSesion_rehidrataEventosOrdenados()
    {
        var opciones = OpcionesInMemory();
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesAplicadas(contexto);
            await repo.AgregarAsync(PenalizacionAplicada.CrearParaEquipo(
                Guid.NewGuid(), sesionId, equipoId, 10, "Motivo 2", Operador, AhoraUtc.AddMinutes(2)),
                CancellationToken.None);
            await repo.AgregarAsync(PenalizacionAplicada.CrearParaEquipo(
                Guid.NewGuid(), sesionId, equipoId, 5, "Motivo 1", Operador, AhoraUtc),
                CancellationToken.None);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoSesiones(opciones))
        {
            var repo = new RepositorioPenalizacionesAplicadas(contexto);
            var recuperadas = await repo.ListarPorSesionAsync(sesionId, CancellationToken.None);

            recuperadas.Should().HaveCount(2);
            recuperadas[0].Motivo.Should().Be("Motivo 1");
            recuperadas[1].Motivo.Should().Be("Motivo 2");
            recuperadas[0].TipoObjetivo.Should().Be(TipoObjetivoPenalizacion.Equipo);
            recuperadas[0].OperadorIdentidadId.Should().Be(Operador);
        }
    }

    [Fact]
    public async Task EventoId_unico_yExistePorEventoId()
    {
        await using var contexto = new ContextoSesiones(OpcionesInMemory());
        var repo = new RepositorioPenalizacionesAplicadas(contexto);
        var eventoId = Guid.NewGuid();

        await repo.AgregarAsync(PenalizacionAplicada.CrearParaParticipante(
            eventoId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            5, "Motivo", Operador, AhoraUtc), CancellationToken.None);
        await contexto.SaveChangesAsync();

        (await repo.ExistePorEventoIdAsync(eventoId, CancellationToken.None)).Should().BeTrue();
        (await repo.ExistePorEventoIdAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task ListarYSumarPorParticipante()
    {
        await using var contexto = new ContextoSesiones(OpcionesInMemory());
        var repo = new RepositorioPenalizacionesAplicadas(contexto);
        var sesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        await repo.AgregarAsync(PenalizacionAplicada.CrearParaParticipante(
            Guid.NewGuid(), sesionId, Guid.NewGuid(), identidadId,
            5, "Motivo A", Operador, AhoraUtc), CancellationToken.None);
        await repo.AgregarAsync(PenalizacionAplicada.CrearParaParticipante(
            Guid.NewGuid(), sesionId, Guid.NewGuid(), identidadId,
            8, "Motivo B", Operador, AhoraUtc.AddMinutes(1)), CancellationToken.None);
        await contexto.SaveChangesAsync();

        var eventos = await repo.ListarPorParticipanteAsync(sesionId, identidadId, CancellationToken.None);
        eventos.Should().HaveCount(2);
        (await repo.SumarPuntosPorParticipanteAsync(sesionId, identidadId, CancellationToken.None))
            .Should().Be(13);
    }

    [Fact]
    public async Task ListarYSumarPorEquipo()
    {
        await using var contexto = new ContextoSesiones(OpcionesInMemory());
        var repo = new RepositorioPenalizacionesAplicadas(contexto);
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await repo.AgregarAsync(PenalizacionAplicada.CrearParaEquipo(
            Guid.NewGuid(), sesionId, equipoId, 20, "Motivo A", Operador, AhoraUtc),
            CancellationToken.None);
        await contexto.SaveChangesAsync();

        var eventos = await repo.ListarPorEquipoAsync(sesionId, equipoId, CancellationToken.None);
        eventos.Should().ContainSingle();
        (await repo.SumarPuntosPorEquipoAsync(sesionId, equipoId, CancellationToken.None))
            .Should().Be(20);
    }

    private static DbContextOptions<ContextoSesiones> OpcionesInMemory()
        => new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase("penalizaciones-" + Guid.NewGuid())
            .Options;
}
