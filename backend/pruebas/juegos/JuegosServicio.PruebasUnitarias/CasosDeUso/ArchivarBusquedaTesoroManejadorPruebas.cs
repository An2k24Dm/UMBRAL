using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU26 + regla "no archivar si hay sesiones vigentes": pruebas del
// manejador de archivado de Búsqueda del Tesoro.
public class ArchivarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();
    private readonly Mock<IClienteSesiones> _clienteSesiones = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ArchivarBusquedaTesoroManejador CrearManejador() =>
        new(_repositorio.Object, _clienteSesiones.Object);

    private static BusquedaTesoro BusquedaActiva()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        busqueda.AgregarMisionAEtapa(etapa.Id, "Misión", "Desc", TipoMision.PistaTexto, "pista");
        busqueda.Activar();
        return busqueda;
    }

    public ArchivarBusquedaTesoroManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ArchivarBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _clienteSesiones
            .Setup(c => c.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.BusquedaTesoro, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_BusquedaSinSesionesVigentes_ArchivaCorrectamente()
    {
        var busqueda = BusquedaActiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new ArchivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        _clienteSesiones.Verify(c => c.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.BusquedaTesoro, busqueda.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(
            r => r.ArchivarBusquedaTesoroAsync(busqueda, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BusquedaConSesionVigente_LanzaExcepcion_YNoPersiste()
    {
        var busqueda = BusquedaActiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);
        _clienteSesiones
            .Setup(c => c.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.BusquedaTesoro, busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accion = async () => await CrearManejador().Handle(
            new ArchivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ContenidoConSesionesVigentesExcepcion>();
        _repositorio.Verify(
            r => r.ArchivarBusquedaTesoroAsync(It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Never);
        busqueda.Estado.Should().Be(EstadoBusqueda.Activa);
    }

    [Fact]
    public async Task Handle_ConsultaSesiones_AntesDeArchivar()
    {
        var busqueda = BusquedaActiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var orden = new List<string>();
        _clienteSesiones
            .Setup(c => c.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.BusquedaTesoro, busqueda.Id, It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("cliente"))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ArchivarBusquedaTesoroAsync(busqueda, It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("repositorio"))
            .Returns(Task.CompletedTask);

        await CrearManejador().Handle(
            new ArchivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        orden.Should().Equal("cliente", "repositorio");
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_LanzaExcepcionNoEncontrado_YNoConsultaSesiones()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        var accion = async () => await CrearManejador().Handle(
            new ArchivarBusquedaTesoroComando(busquedaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
        _clienteSesiones.Verify(c => c.ExisteSesionVigentePorContenidoAsync(
            It.IsAny<TipoJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BusquedaYaArchivada_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaActiva();
        busqueda.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new ArchivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
