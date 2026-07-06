using PartidasServicio.Aplicacion.Estrategias;

namespace PartidasServicio.PruebasUnitarias.Estrategias;

public class CalculadoraPuntajePorTiempoPruebas
{
    private readonly ICalculadoraPuntaje _calculadora = new CalculadoraPuntajePorTiempo();

    [Fact]
    public void Calcular_RespuestaInstantanea_RetornaPuntajeCompleto()
    {
        // factor = 1.0 - 0.5*(0/10000) = 1.0 → round(100 * 1.0) = 100
        var resultado = _calculadora.Calcular(puntajeBase: 100, tiempoTardadoMs: 0, tiempoLimiteMs: 10_000);

        resultado.Should().Be(100);
    }

    [Fact]
    public void Calcular_RespuestaEnExactoLimite_RetornaMitadDelPuntaje()
    {
        // factor = 1.0 - 0.5*(10000/10000) = 0.5 → round(100 * 0.5) = 50
        var resultado = _calculadora.Calcular(puntajeBase: 100, tiempoTardadoMs: 10_000, tiempoLimiteMs: 10_000);

        resultado.Should().Be(50);
    }

    [Fact]
    public void Calcular_RespuestaEnMitadDelTiempo_RetornaFactorIntermedio()
    {
        // factor = 1.0 - 0.5*(5000/10000) = 0.75 → round(100 * 0.75) = 75
        var resultado = _calculadora.Calcular(puntajeBase: 100, tiempoTardadoMs: 5_000, tiempoLimiteMs: 10_000);

        resultado.Should().Be(75);
    }

    [Fact]
    public void Calcular_TiempoSuperaLimite_FactorClampA050()
    {
        // tiempo > límite → min(tiempo,limite) = limite → factor = 0.5
        var resultado = _calculadora.Calcular(puntajeBase: 100, tiempoTardadoMs: 99_999, tiempoLimiteMs: 10_000);

        resultado.Should().Be(50);
    }

    [Fact]
    public void Calcular_PuntajeBaseDistinto_EscalaCorrectamente()
    {
        // puntajeBase=200, tiempo=0 → factor=1.0 → 200
        var resultado = _calculadora.Calcular(puntajeBase: 200, tiempoTardadoMs: 0, tiempoLimiteMs: 10_000);

        resultado.Should().Be(200);
    }

    [Fact]
    public void Calcular_ResultadoSiemprePositivo()
    {
        var resultado = _calculadora.Calcular(puntajeBase: 10, tiempoTardadoMs: 10_000, tiempoLimiteMs: 10_000);

        resultado.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calcular_PuntajeBaseCero_RetornaCero()
    {
        var resultado = _calculadora.Calcular(puntajeBase: 0, tiempoTardadoMs: 0, tiempoLimiteMs: 10_000);

        resultado.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 10_000, 100)]       // factor 1.0
    [InlineData(2_500, 10_000, 88)]    // factor 0.875 → round(100*0.875) = 88
    [InlineData(5_000, 10_000, 75)]    // factor 0.75
    [InlineData(7_500, 10_000, 62)]    // factor 0.625 → round(100*0.625) = 62 (banker's rounding)
    [InlineData(10_000, 10_000, 50)]   // factor 0.5
    public void Calcular_VariasTiempos_RetornaFactorEsperado(
        long tiempoMs, int limiteMs, int esperado)
    {
        var resultado = _calculadora.Calcular(puntajeBase: 100, tiempoTardadoMs: tiempoMs, tiempoLimiteMs: limiteMs);

        resultado.Should().Be(esperado);
    }
}
