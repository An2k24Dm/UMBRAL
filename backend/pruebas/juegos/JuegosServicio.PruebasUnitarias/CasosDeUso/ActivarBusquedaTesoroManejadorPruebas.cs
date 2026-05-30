using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU26: pruebas del manejador de activación de búsqueda del tesoro.
public class ActivarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ActivarBusquedaTesoroManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConEtapaYMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        busqueda.AgregarMisionAEtapa(etapa.Id, "Misión", "Desc", TipoMision.PistaTexto, "pista");
        return busqueda;
    }

    public ActivarBusquedaTesoroManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ActivarBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaConEtapasYMisiones_LlamaActivarAsyncUnaVez()
    {
        var busqueda = BusquedaConEtapaYMision();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new ActivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        _repositorio.Verify(
            r => r.ActivarBusquedaTesoroAsync(busqueda, It.IsAny<CancellationToken>()),
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
            new ActivarBusquedaTesoroComando(busquedaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_BusquedaSinEtapas_LanzaExcepcionDominio()
    {
        var busquedaVacia = BusquedaTesoro.Crear("Búsqueda vacía", "Descripción", Guid.NewGuid(), FechaFija);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaVacia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busquedaVacia);

        var accion = async () => await CrearManejador().Handle(
            new ActivarBusquedaTesoroComando(busquedaVacia.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
