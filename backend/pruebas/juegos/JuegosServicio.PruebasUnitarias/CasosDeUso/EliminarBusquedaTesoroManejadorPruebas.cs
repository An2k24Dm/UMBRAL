using JuegosServicio.Aplicacion.Comandos.EliminarBusquedaTesoro;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class EliminarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private EliminarBusquedaTesoroManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object);

    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    public EliminarBusquedaTesoroManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarBusquedaTesoroAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositorioMisiones
            .Setup(r => r.EsContenidoUsadoEnEtapaAsync(
                TipoModoDeJuego.BusquedaTesoro, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_BusquedaInactivaSinMisiones_EliminaCorrectamente()
    {
        var busqueda = BusquedaInactiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(new EliminarBusquedaTesoroComando(busqueda.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.EliminarBusquedaTesoroAsync(busqueda.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarBusquedaTesoroComando(busquedaId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
        _repositorio.Verify(
            r => r.EliminarBusquedaTesoroAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaInactiva();
        // La regla nueva exige una pista para activar.
        busqueda.AgregarPista("Pista única");
        busqueda.Activar();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarBusquedaTesoroComando(busqueda.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
        _repositorio.Verify(
            r => r.EliminarBusquedaTesoroAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BusquedaUsadaEnMision_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaInactiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);
        _repositorioMisiones
            .Setup(r => r.EsContenidoUsadoEnEtapaAsync(
                TipoModoDeJuego.BusquedaTesoro, busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarBusquedaTesoroComando(busqueda.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>()
            .WithMessage("*misiones*");
        _repositorio.Verify(
            r => r.EliminarBusquedaTesoroAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
