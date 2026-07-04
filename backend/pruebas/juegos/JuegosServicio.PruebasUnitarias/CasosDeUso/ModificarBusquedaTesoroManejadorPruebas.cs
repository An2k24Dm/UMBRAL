using JuegosServicio.Aplicacion.Comandos.ModificarBusquedaTesoro;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class ModificarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();
    private readonly Mock<IValidador<ModificarBusquedaTesoroComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarBusquedaTesoroManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object, _validador.Object);

    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Original", "Descripción original", Guid.NewGuid(), FechaFija);

    private static ModificarBusquedaTesoroDto DtoValido() =>
        new() { Nombre = "Búsqueda Modificada", Descripcion = "Nueva descripción", Tiempo = 30, Puntaje = 50 };

    public ModificarBusquedaTesoroManejadorPruebas()
    {
        _repositorioMisiones.Setup(r => r.EsContenidoUsadoEnMisionActivaAsync(
            It.IsAny<TipoModoDeJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _validador.Setup(v => v.Validar(It.IsAny<ModificarBusquedaTesoroComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
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
        busqueda.Tiempo.Valor.Should().Be(30);
        busqueda.Puntaje.Valor.Should().Be(50);
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
