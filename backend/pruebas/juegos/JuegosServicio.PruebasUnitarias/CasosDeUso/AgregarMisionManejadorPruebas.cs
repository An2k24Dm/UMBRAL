using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU23: pruebas del manejador para agregar una misión a una etapa.
public class AgregarMisionManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private AgregarMisionManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConEtapa(out Guid etapaId)
    {
        var busqueda = BusquedaTesoro.Crear(
            "Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa", 1);
        etapaId = etapa.Id;
        return busqueda;
    }

    private static AgregarMisionComando ComandoValido(Guid busquedaId, Guid etapaId) =>
        new(busquedaId, etapaId, new AgregarMisionDto
        {
            Titulo = "Encuentra la estatua",
            Descripcion = "Busca la estatua principal del parque",
            Tipo = 0, // PistaTexto
            PistaClave = "Mira hacia el norte"
        });

    public AgregarMisionManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.AgregarMisionAsync(
                It.IsAny<Guid>(), It.IsAny<Mision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaYEtapaExistentes_RetornaIdDeMisionNoVacio()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var resultado = await CrearManejador()
            .Handle(ComandoValido(busqueda.Id, etapaId), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BusquedaYEtapaExistentes_LlamaAgregarMisionAsyncUnaVez()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador()
            .Handle(ComandoValido(busqueda.Id, etapaId), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarMisionAsync(etapaId, It.IsAny<Mision>(), It.IsAny<CancellationToken>()),
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
            await CrearManejador()
                .Handle(ComandoValido(busquedaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapa(out _);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () =>
            await CrearManejador()
                .Handle(ComandoValido(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }
}
