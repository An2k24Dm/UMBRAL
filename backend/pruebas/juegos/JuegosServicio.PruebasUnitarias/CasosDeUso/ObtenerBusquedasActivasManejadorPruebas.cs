using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class ObtenerBusquedasActivasManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private ObtenerBusquedasActivasManejador CrearManejador() => new(_repositorio.Object);

    [Fact]
    public async Task Handle_DevuelveLaListaDelRepositorio()
    {
        var lista = new List<BusquedaTesoroResumenDto>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Búsqueda A", TotalPistas = 2 },
            new() { Id = Guid.NewGuid(), Nombre = "Búsqueda B", TotalPistas = 0 }
        };
        _repositorio
            .Setup(r => r.ObtenerBusquedasActivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(lista);

        var resultado = await CrearManejador()
            .Handle(new ObtenerBusquedasActivasConsulta(), CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].Nombre.Should().Be("Búsqueda A");
    }

    [Fact]
    public async Task Handle_SinResultados_DevuelveListaVacia()
    {
        _repositorio
            .Setup(r => r.ObtenerBusquedasActivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusquedaTesoroResumenDto>());

        var resultado = await CrearManejador()
            .Handle(new ObtenerBusquedasActivasConsulta(), CancellationToken.None);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LlamaAlRepositorioUnaVez()
    {
        _repositorio
            .Setup(r => r.ObtenerBusquedasActivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusquedaTesoroResumenDto>());

        await CrearManejador().Handle(new ObtenerBusquedasActivasConsulta(), CancellationToken.None);

        _repositorio.Verify(r => r.ObtenerBusquedasActivasAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
