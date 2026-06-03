using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class ModificarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarBusquedaTesoroManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Original", "Descripción original", Guid.NewGuid(), FechaFija);

    private static ModificarBusquedaTesoroDto DtoValido() =>
        new() { Nombre = "Búsqueda Modificada", Descripcion = "Nueva descripción", Tiempo = 120, Puntaje = 50 };

    public ModificarBusquedaTesoroManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ActualizarBusquedaAsync(It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaInactiva_LlamaActualizarAsyncUnaVez()
    {
        var busqueda = BusquedaInactiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new ModificarBusquedaTesoroComando(busqueda.Id, DtoValido()), CancellationToken.None);

        _repositorio.Verify(
            r => r.ActualizarBusquedaAsync(busqueda, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BusquedaInactiva_ActualizaDatosDominio()
    {
        var busqueda = BusquedaInactiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new ModificarBusquedaTesoroComando(busqueda.Id, DtoValido()), CancellationToken.None);

        busqueda.Nombre.Should().Be("Búsqueda Modificada");
        busqueda.Descripcion.Should().Be("Nueva descripción");
        busqueda.Tiempo.Should().Be(120);
        busqueda.Puntaje.Should().Be(50);
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        var accion = async () => await CrearManejador()
            .Handle(new ModificarBusquedaTesoroComando(busquedaId, DtoValido()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
        _repositorio.Verify(
            r => r.ActualizarBusquedaAsync(It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaInactiva();
        busqueda.AgregarPista("Pista única"); // requerida para activar
        busqueda.Activar();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador()
            .Handle(new ModificarBusquedaTesoroComando(busqueda.Id, DtoValido()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
        _repositorio.Verify(
            r => r.ActualizarBusquedaAsync(It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
