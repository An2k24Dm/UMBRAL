using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Infraestructura.Persistencia;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public sealed class ModeloRankingPruebas
{
    [Fact]
    public void Dominio_noContienePuntajeEtapaParticipante()
    {
        var ensambladoDominio = typeof(Ranking).Assembly;

        ensambladoDominio
            .GetType("RankingServicio.Dominio.Entidades.PuntajeEtapaParticipante")
            .Should().BeNull();
        typeof(Ranking).GetField("_puntajesEtapa",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
            .Should().BeNull();
        typeof(Ranking).GetProperty("PuntajesEtapa").Should().BeNull();
        typeof(Ranking).GetMethod("RegistrarPuntajeEtapa").Should().BeNull();
        typeof(Ranking).GetMethod("PuntajesEtapaDeParticipante").Should().BeNull();
    }

    [Fact]
    public void ContextoRanking_noMapeaTablaRankingPuntajesEtapa()
    {
        var opciones = new DbContextOptionsBuilder<ContextoRanking>()
            .UseNpgsql("Host=localhost;Database=umbral_model_test;Username=test;Password=test")
            .Options;
        using var contexto = new ContextoRanking(opciones);

        var tablas = contexto.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(nombre => nombre is not null)
            .ToArray();

        tablas.Should().NotContain("ranking_puntajes_etapa");
        contexto.Model.FindEntityType(typeof(Ranking))!
            .FindNavigation("PuntajesEtapa")
            .Should().BeNull();
    }
}
