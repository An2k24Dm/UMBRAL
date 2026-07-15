using JuegosServicio.Infraestructura.Persistencia;

namespace JuegosServicio.PruebasUnitarias.Infraestructura;

public class RepositorioMisionesTiempoPruebas
{
    [Fact]
    public void BusquedaDeCincoMinutos_ProduceTiempoEstimadoDeTrescientosSegundos()
    {
        var tiempoEstimado = RepositorioMisiones.CalcularTiempoEstimadoEtapaSegundos(
            esBusquedaTesoro: true,
            tiempoBusquedaMinutos: 5,
            tiempoTriviaSegundos: 0);

        tiempoEstimado.Should().Be(300);
    }

    [Fact]
    public void MisionConUnaBusquedaDeCincoMinutos_ProduceTiempoTotalDeTrescientosSegundos()
    {
        var tiemposEtapas = new[]
        {
            RepositorioMisiones.CalcularTiempoEstimadoEtapaSegundos(
                esBusquedaTesoro: true,
                tiempoBusquedaMinutos: 5,
                tiempoTriviaSegundos: 0)
        };

        tiemposEtapas.Sum().Should().Be(300);
    }

    [Fact]
    public void MisionConTriviaBusquedaYTrivia_SumaTodoEnSegundos()
    {
        var tiemposEtapas = new[]
        {
            RepositorioMisiones.CalcularTiempoEstimadoEtapaSegundos(
                esBusquedaTesoro: false,
                tiempoBusquedaMinutos: 0,
                tiempoTriviaSegundos: 120),
            RepositorioMisiones.CalcularTiempoEstimadoEtapaSegundos(
                esBusquedaTesoro: true,
                tiempoBusquedaMinutos: 5,
                tiempoTriviaSegundos: 0),
            RepositorioMisiones.CalcularTiempoEstimadoEtapaSegundos(
                esBusquedaTesoro: false,
                tiempoBusquedaMinutos: 0,
                tiempoTriviaSegundos: 60)
        };

        tiemposEtapas.Sum().Should().Be(480);
    }
}
