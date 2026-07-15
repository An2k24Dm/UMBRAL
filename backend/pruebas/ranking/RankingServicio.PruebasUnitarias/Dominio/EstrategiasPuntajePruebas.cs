using FluentAssertions;
using RankingServicio.Dominio.Estrategias;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Dominio;

public sealed class EstrategiasPuntajePruebas
{
    private readonly EstrategiaPuntajeTriviaPorTiempo _trivia = new();

    [Fact]
    public void Trivia_incorrecta_devuelveCero()
    {
        var puntaje = _trivia.Calcular(new(false, 5, 0, 10_000));

        puntaje.Valor.Should().Be(0);
    }

    [Fact]
    public void Trivia_tiempoMayorOIgualAlLimite_devuelveCero()
    {
        _trivia.Calcular(new(true, 5, 10_000, 10_000)).Valor.Should().Be(0);
        _trivia.Calcular(new(true, 5, 11_000, 10_000)).Valor.Should().Be(0);
    }

    [Fact]
    public void Trivia_tiempoLimiteInvalido_devuelveCero()
    {
        _trivia.Calcular(new(true, 5, 0, 0)).Valor.Should().Be(0);
        _trivia.Calcular(new(true, 5, 0, -1)).Valor.Should().Be(0);
    }

    [Fact]
    public void Trivia_primerTramo_conservaPuntajeBase()
    {
        _trivia.Calcular(new(true, 5, 0, 10_000)).Valor.Should().Be(5);
        _trivia.Calcular(new(true, 5, 1_999, 10_000)).Valor.Should().Be(5);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1_999, 5)]
    [InlineData(2_000, 4)]
    [InlineData(3_999, 4)]
    [InlineData(4_000, 3)]
    [InlineData(5_999, 3)]
    [InlineData(6_000, 2)]
    [InlineData(7_999, 2)]
    [InlineData(8_000, 1)]
    [InlineData(9_999, 1)]
    [InlineData(10_000, 0)]
    public void Trivia_conservaFormulaAnteriorPorTramos(int tiempoMs, int esperado)
    {
        var puntaje = _trivia.Calcular(new(true, 5, tiempoMs, 10_000));

        puntaje.Valor.Should().Be(esperado);
    }

    [Fact]
    public void Trivia_nuevaEstrategiaProduceLoMismoQueFormulaAnterior()
    {
        var entradas = new[]
        {
            new ContextoCalculoPuntajeTrivia(false, 5, 0, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 5, 0, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 5, 2_000, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 5, 4_000, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 5, 6_000, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 5, 8_000, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 5, 10_000, 10_000),
            new ContextoCalculoPuntajeTrivia(true, 13, 3_500, 10_000)
        };

        foreach (var contexto in entradas)
        {
            _trivia.Calcular(contexto).Valor.Should().Be(FormulaAnterior(contexto));
        }
    }

    [Fact]
    public void Tesoro_evidenciaValida_unicoCompetidor_devuelvePuntajeBase()
    {
        var estrategia = new EstrategiaPuntajeBusquedaTesoro();

        estrategia.Calcular(new(true, 50, 1, 1, 0, 300_000)).Valor.Should().Be(50);
    }

    [Fact]
    public void Tesoro_evidenciaInvalida_devuelveCero()
    {
        var estrategia = new EstrategiaPuntajeBusquedaTesoro();

        estrategia.Calcular(new(false, 50, 1, 3, 0, 300_000)).Valor.Should().Be(0);
    }

    private static int FormulaAnterior(ContextoCalculoPuntajeTrivia contexto)
    {
        if (!contexto.EsCorrecta) return 0;
        if (contexto.TiempoLimiteMs <= 0 || contexto.TiempoTardadoMs >= contexto.TiempoLimiteMs)
            return 0;

        var tamanoTramo = contexto.TiempoLimiteMs / 5;
        var tramo = tamanoTramo > 0 ? contexto.TiempoTardadoMs / tamanoTramo : 0;
        var factor = 1m - tramo * 0.2m;
        return Math.Max(0, (int)(contexto.PuntajeBase * factor));
    }
}
