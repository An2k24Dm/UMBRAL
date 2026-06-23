using JuegosServicio.Aplicacion.Consultas.ObtenerDetalleTrivia;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU15: pruebas del manejador de detalle de trivia.
public class ObtenerDetalleTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();

    private ObtenerDetalleTriviaManejador CrearManejador() =>
        new(_repositorio.Object);

    [Fact]
    public async Task Handle_TriviaExistente_DevuelveDetalle()
    {
        var triviaId = Guid.NewGuid();
        var detalle = new TriviaDetalleDto { Id = triviaId, Nombre = "Trivia de Geografía" };
        _repositorio
            .Setup(r => r.ObtenerDetalleTriviaAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detalle);

        var resultado = await CrearManejador()
            .Handle(new ObtenerDetalleTriviaConsulta(triviaId), CancellationToken.None);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(triviaId);
        resultado.Nombre.Should().Be("Trivia de Geografía");
    }

    [Fact]
    public async Task Handle_TriviaInexistente_DevuelveNull()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerDetalleTriviaAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaDetalleDto?)null);

        var resultado = await CrearManejador()
            .Handle(new ObtenerDetalleTriviaConsulta(triviaId), CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PropagaTriviaIdAlRepositorio()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerDetalleTriviaAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaDetalleDto?)null);

        await CrearManejador()
            .Handle(new ObtenerDetalleTriviaConsulta(triviaId), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerDetalleTriviaAsync(triviaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
