using JuegosServicio.Aplicacion.Comandos.ActivarBusquedaTesoro;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class ActivarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ActivarBusquedaTesoroManejador CrearManejador() =>
        new(_repositorio.Object, Mock.Of<IRegistroLogsAplicacion>());

    // Búsqueda Inactiva con al menos una pista: la nueva regla del
    // ERS exige una pista antes de activar.
    private static BusquedaTesoro BusquedaInactiva()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816);
        return busqueda;
    }

    [Fact]
    public async Task Handle_BusquedaSinPistas_LanzaExcepcionDominio_YNoActiva()
    {
        var busqueda = BusquedaTesoro.Crear("Sin pistas", "Descripción", Guid.NewGuid(), FechaFija);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new ActivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<ExcepcionDominio>()
            .WithMessage("La búsqueda del tesoro debe tener una coordenada GPS del tesoro para poder activarse.");
        _repositorio.Verify(
            r => r.ActivarBusquedaTesoroAsync(It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public ActivarBusquedaTesoroManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ActivarBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaInactiva_LlamaActivarAsyncUnaVez()
    {
        var busqueda = BusquedaInactiva();
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
    public async Task Handle_BusquedaInexistente_NoLlamaActivarAsync()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        try { await CrearManejador().Handle(
            new ActivarBusquedaTesoroComando(busquedaId, Guid.NewGuid()), CancellationToken.None); }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.ActivarBusquedaTesoroAsync(It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
