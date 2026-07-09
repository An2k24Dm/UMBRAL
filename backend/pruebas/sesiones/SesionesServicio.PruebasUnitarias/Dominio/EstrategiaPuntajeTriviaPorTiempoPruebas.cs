using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Estrategias;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Verifica la política de puntuación por tiempo (5 tramos de 20 %) de forma
// aislada, sin el manejador. Conserva el comportamiento exacto anterior.
public class EstrategiaPuntajeTriviaPorTiempoPruebas
{
    private readonly IEstrategiaCalculoPuntajeTrivia _estrategia = new EstrategiaPuntajeTriviaPorTiempo();

    private int Calcular(bool correcta, int puntajeBase, int tiempoMs, int limiteMs)
        => _estrategia.Calcular(new ContextoCalculoPuntajeTrivia(correcta, puntajeBase, tiempoMs, limiteMs));

    [Fact] // (21)
    public void RespuestaIncorrecta_DevuelveCero()
        => Calcular(correcta: false, puntajeBase: 100, tiempoMs: 0, limiteMs: 10_000)
            .Should().Be(0);

    [Fact] // (22)
    public void TiempoIgualAlLimite_DevuelveCero()
        => Calcular(correcta: true, puntajeBase: 100, tiempoMs: 10_000, limiteMs: 10_000)
            .Should().Be(0);

    [Fact] // (23)
    public void TiempoMayorAlLimite_DevuelveCero()
        => Calcular(correcta: true, puntajeBase: 100, tiempoMs: 12_000, limiteMs: 10_000)
            .Should().Be(0);

    [Fact]
    public void LimiteCeroOInvalido_DevuelveCero()
        => Calcular(correcta: true, puntajeBase: 100, tiempoMs: 0, limiteMs: 0)
            .Should().Be(0);

    // (24) Los cinco tramos, con puntaje base 100 y límite 10 s. Se conserva
    // EXACTAMENTE el comportamiento previo, incluida la truncación por punto
    // flotante en los límites de tramo (tramo 3 → 39, tramo 4 → 19), tal como
    // ya lo documentaban las pruebas originales del manejador.
    [Theory]
    [InlineData(1_000, 100)]  // tramo 0 (0-2s)
    [InlineData(2_001, 80)]   // tramo 1 (2-4s)
    [InlineData(4_001, 60)]   // tramo 2 (4-6s)
    [InlineData(6_001, 39)]   // tramo 3 (6-8s) → truncación
    [InlineData(8_001, 19)]   // tramo 4 (8-10s) → truncación
    [InlineData(10_000, 0)]   // >= límite
    public void CincoTramos_DevuelvenPuntajeEsperado(int tiempoMs, int esperado)
        => Calcular(correcta: true, puntajeBase: 100, tiempoMs: tiempoMs, limiteMs: 10_000)
            .Should().Be(esperado);

    [Theory] // (25) El puntaje nunca es negativo, sea cual sea la entrada.
    [InlineData(1, 0, 10_000)]
    [InlineData(5, 9_999, 10_000)]
    [InlineData(100, 5_000, 10_000)]
    [InlineData(3, 7_500, 10_000)]
    public void PuntajeNuncaEsNegativo(int puntajeBase, int tiempoMs, int limiteMs)
        => Calcular(correcta: true, puntajeBase: puntajeBase, tiempoMs: tiempoMs, limiteMs: limiteMs)
            .Should().BeGreaterThanOrEqualTo(0);
}
