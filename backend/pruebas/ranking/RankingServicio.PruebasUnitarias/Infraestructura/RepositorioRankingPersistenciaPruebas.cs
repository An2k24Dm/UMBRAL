using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.ObjetosValor;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.Persistencia.Repositorios;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public sealed class RepositorioRankingPersistenciaPruebas
{
    // HU52 — Persistencia de penalización individual: puntaje negativo y puntos
    // penalizados sobreviven al ciclo de guardado/rehidratación.
    [Fact]
    public async Task PenalizacionIndividual_persisteNegativoYPuntosPenalizados()
    {
        var opciones = OpcionesInMemory();
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var identidadId = Guid.NewGuid();

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = Ranking.Crear(sesionId);
            ranking.RegistrarPuntajeParticipante(participanteSesionId, identidadId, null, 3);
            ranking.AplicarPenalizacionParticipante(
                participanteSesionId, identidadId, CantidadPenalizacion.Crear(5));
            await contexto.Rankings.AddAsync(ranking);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = await new RepositorioRanking(contexto)
                .ObtenerPorSesionAsync(sesionId, CancellationToken.None);
            var participante = ranking!.Participantes.Single();
            participante.Puntaje.Valor.Should().Be(-2);
            participante.PuntosPenalizados.Should().Be(5);
        }
    }

    // HU52 — Persistencia de penalización grupal: el equipo conserva su puntaje
    // (posible negativo) y puntos penalizados; los participantes no cambian.
    [Fact]
    public async Task PenalizacionGrupal_persisteEquipoYNoTocaParticipantes()
    {
        var opciones = OpcionesInMemory();
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = Ranking.Crear(sesionId);
            ranking.RegistrarPuntajeParticipante(participanteSesionId, Guid.NewGuid(), equipoId, 10);
            ranking.AplicarPenalizacionEquipo(equipoId, CantidadPenalizacion.Crear(30));
            await contexto.Rankings.AddAsync(ranking);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = await new RepositorioRanking(contexto)
                .ObtenerPorSesionAsync(sesionId, CancellationToken.None);
            var equipo = ranking!.Equipos.Single(e => e.EquipoId == equipoId);
            equipo.Puntaje.Valor.Should().Be(-20);
            equipo.PuntosPenalizados.Should().Be(30);
            ranking.Participantes.Single().Puntaje.Valor.Should().Be(10);
            ranking.Participantes.Single().PuntosPenalizados.Should().Be(0);
        }
    }

    // HU52 — Los registros sin penalización mantienen PuntosPenalizados = 0
    // (valor por defecto de la migración).
    [Fact]
    public async Task SinPenalizacion_puntosPenalizadosEsCeroPorDefecto()
    {
        var opciones = OpcionesInMemory();
        var sesionId = Guid.NewGuid();

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = Ranking.Crear(sesionId);
            ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), Guid.NewGuid(), null, 7);
            await contexto.Rankings.AddAsync(ranking);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = await new RepositorioRanking(contexto)
                .ObtenerPorSesionAsync(sesionId, CancellationToken.None);
            ranking!.Participantes.Single().PuntosPenalizados.Should().Be(0);
        }
    }

    private static DbContextOptions<ContextoRanking> OpcionesInMemory()
        => new DbContextOptionsBuilder<ContextoRanking>()
            .UseInMemoryDatabase($"ranking-{Guid.NewGuid()}")
            .Options;

    [Fact]
    public async Task RankingConEquipoPreexistente_persisteParticipanteNuevoAgregadoPorPuntaje()
    {
        var opciones = new DbContextOptionsBuilder<ContextoRanking>()
            .UseInMemoryDatabase($"ranking-grupal-{Guid.NewGuid()}")
            .Options;
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var participanteIdentidadId = Guid.NewGuid();

        await using (var contexto = new ContextoRanking(opciones))
        {
            var ranking = Ranking.Crear(sesionId);
            ranking.RegistrarEquipo(equipoId);
            await contexto.Rankings.AddAsync(ranking);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoRanking(opciones))
        {
            var repositorio = new RepositorioRanking(contexto);
            var ranking = await repositorio.ObtenerPorSesionAsync(sesionId, CancellationToken.None);

            ranking.Should().NotBeNull();
            ranking!.RegistrarPuntajeParticipante(
                participanteSesionId,
                participanteIdentidadId,
                equipoId,
                5);

            await repositorio.ActualizarAsync(ranking, CancellationToken.None);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = new ContextoRanking(opciones))
        {
            var repositorio = new RepositorioRanking(contexto);
            var ranking = await repositorio.ObtenerPorSesionAsync(sesionId, CancellationToken.None);

            ranking.Should().NotBeNull();
            ranking!.Participantes.Should().ContainSingle(p =>
                p.ParticipanteSesionId == participanteSesionId &&
                p.ParticipanteIdentidadId == participanteIdentidadId &&
                p.EquipoId == equipoId &&
                p.Puntaje.Valor == 5);
            ranking.Equipos.Single(e => e.EquipoId == equipoId)
                .Puntaje.Valor.Should().Be(5);
        }
    }
}
