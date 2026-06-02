using JuegosServicio.Dominio.Enums;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU23: pruebas del manejador para asignar la misión a una búsqueda del tesoro.
public class AgregarMisionManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private AgregarMisionManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaSinMision() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    private static AgregarMisionComando ComandoValido(Guid busquedaId) =>
        new(busquedaId, new AgregarMisionDto
        {
            Titulo = "Encuentra la estatua",
            Descripcion = "Busca la estatua principal del parque",
            PistaClave = "Mira hacia el norte"
        });

    public AgregarMisionManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.AsignarMisionAsync(
                It.IsAny<Guid>(), It.IsAny<Mision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaExistente_RetornaIdDeMisionNoVacio()
    {
        var busqueda = BusquedaSinMision();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var resultado = await CrearManejador()
            .Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BusquedaExistente_LlamaAsignarMisionAsyncUnaVez()
    {
        var busqueda = BusquedaSinMision();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.AsignarMisionAsync(busqueda.Id, It.IsAny<Mision>(), It.IsAny<CancellationToken>()),
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
    public async Task Handle_BusquedaYaTieneMision_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaSinMision();
        busqueda.AsignarMision("Misión existente", "Desc", TipoMision.PalabraClave, "clave");
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () =>
            await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
