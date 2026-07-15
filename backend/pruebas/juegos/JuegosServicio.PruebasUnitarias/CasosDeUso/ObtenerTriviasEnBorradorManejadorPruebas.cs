using JuegosServicio.Aplicacion.Consultas.ObtenerTriviasEnBorrador;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU15: pruebas del manejador de listado de trivias en borrador.
public class ObtenerTriviasEnBorradorManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();

    private ObtenerTriviasEnBorradorManejador CrearManejador() =>
        new(_repositorio.Object);

    [Fact]
    public async Task Handle_ConFiltroOperador_PropagaOperadorIdAlRepositorio()
    {
        var operadorId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerTriviasEnBorradorAsync(operadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TriviaResumenDto>());

        await CrearManejador()
            .Handle(new ObtenerTriviasEnBorradorConsulta(operadorId), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerTriviasEnBorradorAsync(operadorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SinFiltro_PropagaNullAlRepositorio()
    {
        _repositorio
            .Setup(r => r.ObtenerTriviasEnBorradorAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TriviaResumenDto>());

        await CrearManejador()
            .Handle(new ObtenerTriviasEnBorradorConsulta(null), CancellationToken.None);

        _repositorio.Verify(
            r => r.ObtenerTriviasEnBorradorAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DevuelveLaListaDelRepositorio()
    {
        var lista = new List<TriviaResumenDto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Trivia A" },
            new() { Id = Guid.NewGuid(), Nombre = "Trivia B" }
        };
        _repositorio
            .Setup(r => r.ObtenerTriviasEnBorradorAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lista);

        var resultado = await CrearManejador()
            .Handle(new ObtenerTriviasEnBorradorConsulta(null), CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].Nombre.Should().Be("Trivia A");
    }

    [Fact]
    public async Task Handle_SinResultados_DevuelveListaVacia()
    {
        _repositorio
            .Setup(r => r.ObtenerTriviasEnBorradorAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TriviaResumenDto>());

        var resultado = await CrearManejador()
            .Handle(new ObtenerTriviasEnBorradorConsulta(Guid.NewGuid()), CancellationToken.None);

        resultado.Should().BeEmpty();
    }
}
