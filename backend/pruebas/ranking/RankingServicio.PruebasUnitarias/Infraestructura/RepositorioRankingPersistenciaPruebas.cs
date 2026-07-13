using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.Persistencia.Repositorios;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public sealed class RepositorioRankingPersistenciaPruebas
{
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
