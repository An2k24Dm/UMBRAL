using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU24: pruebas del manejador de modificación de etapa.
public class ModificarEtapaManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarEtapaManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConEtapa(out Guid etapaId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa original", "Descripción original");
        etapaId = etapa.Id;
        return busqueda;
    }

    public ModificarEtapaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ModificarEtapaAsync(
                It.IsAny<Guid>(), It.IsAny<Etapa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaYEtapaExistentes_LlamaModificarEtapaAsyncUnaVez()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new ModificarEtapaComando(busqueda.Id, etapaId,
                new ModificarEtapaDto { NuevoTitulo = "Nuevo", NuevaDescripcion = "Desc" }),
            CancellationToken.None);

        _repositorio.Verify(
            r => r.ModificarEtapaAsync(busqueda.Id, It.IsAny<Etapa>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        var accion = async () => await CrearManejador().Handle(
            new ModificarEtapaComando(busquedaId, Guid.NewGuid(),
                new ModificarEtapaDto { NuevoTitulo = "Título", NuevaDescripcion = "Desc" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapa(out _);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new ModificarEtapaComando(busqueda.Id, Guid.NewGuid(),
                new ModificarEtapaDto { NuevoTitulo = "Título", NuevaDescripcion = "Desc" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }
}
