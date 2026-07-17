using SesionesServicio.Aplicacion.Consultas.ObtenerResultadoPuntaje;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.PruebasUnitarias.Consultas;

public sealed class ObtenerResultadoPuntajeManejadorPruebas
{
    private const string TipoResultado = "ranking.puntaje_actualizado";

    [Fact]
    public async Task SinResultadoProcesado_devuelvePendiente()
    {
        var eventoId = Guid.NewGuid();
        var respuestas = new Mock<IRepositorioRespuestasTrivia>();
        var resultados = new Mock<IRepositorioResultadosRankingProcesados>();
        respuestas.Setup(r => r.ObtenerPuntajeGanadoPorEventoAsync(
                eventoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        resultados.Setup(r => r.ExisteAsync(eventoId, TipoResultado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dto = await new ObtenerResultadoPuntajeManejador(
                respuestas.Object, resultados.Object)
            .Handle(new ObtenerResultadoPuntajeConsulta(eventoId), CancellationToken.None);

        dto.Procesado.Should().BeFalse();
        dto.PuntajeGanado.Should().BeNull();
    }

    [Fact]
    public async Task ResultadoProcesado_conPuntajeCero_devuelveCero()
    {
        var eventoId = Guid.NewGuid();
        var respuestas = new Mock<IRepositorioRespuestasTrivia>();
        var resultados = new Mock<IRepositorioResultadosRankingProcesados>();
        respuestas.Setup(r => r.ObtenerPuntajeGanadoPorEventoAsync(
                eventoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        resultados.Setup(r => r.ExisteAsync(eventoId, TipoResultado, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = await new ObtenerResultadoPuntajeManejador(
                respuestas.Object, resultados.Object)
            .Handle(new ObtenerResultadoPuntajeConsulta(eventoId), CancellationToken.None);

        dto.Procesado.Should().BeTrue();
        dto.PuntajeGanado.Should().Be(0);
    }
}
