using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU22/HU27: pruebas del manejador para agregar una etapa a una búsqueda del tesoro.
public class AgregarEtapaManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private AgregarEtapaManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaEnBorrador() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    private static AgregarEtapaComando ComandoValido(Guid busquedaId, int orden = 1) =>
        new(busquedaId, new AgregarEtapaDto
        {
            Titulo = "Etapa 1 — Entrada al parque",
            Descripcion = "Busca la estatua principal",
            Orden = orden
        });

    public AgregarEtapaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.AgregarEtapaAsync(
                It.IsAny<Guid>(), It.IsAny<Etapa>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaExistente_RetornaIdDeEtapaNoVacio()
    {
        var busqueda = BusquedaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var resultado = await CrearManejador()
            .Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BusquedaExistente_LlamaAgregarEtapaAsyncUnaVez()
    {
        var busqueda = BusquedaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarEtapaAsync(busqueda.Id, It.IsAny<Etapa>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        var accion = async () =>
            await CrearManejador().Handle(ComandoValido(busquedaId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_NoLlamaAgregarEtapaAsync()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        try { await CrearManejador().Handle(ComandoValido(busquedaId), CancellationToken.None); }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.AgregarEtapaAsync(
                It.IsAny<Guid>(), It.IsAny<Etapa>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // HU27 — orden duplicado: el dominio lanza ExcepcionDominio y no se persiste
    [Fact]
    public async Task Handle_OrdenDuplicado_LanzaExcepcionDominio_YNoLlamaAgregarEtapaAsync()
    {
        var busqueda = BusquedaEnBorrador();
        busqueda.AgregarEtapa("Etapa existente", "Ya ocupa el orden 1", 1);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () =>
            await CrearManejador().Handle(ComandoValido(busqueda.Id, orden: 1), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
        _repositorio.Verify(
            r => r.AgregarEtapaAsync(
                It.IsAny<Guid>(), It.IsAny<Etapa>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
