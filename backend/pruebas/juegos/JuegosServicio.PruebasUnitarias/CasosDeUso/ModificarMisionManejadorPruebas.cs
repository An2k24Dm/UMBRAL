using JuegosServicio.Dominio.Enums;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU25: pruebas del manejador de modificación de misión.
public class ModificarMisionManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarMisionManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Misión", "Desc", TipoMision.PalabraClave, "pista");
        return busqueda;
    }

    private static ModificarMisionDto DtoValido() => new()
    {
        NuevoTitulo = "Título modificado",
        NuevaDescripcion = "Descripción modificada",
        NuevaPistaClave = "nueva-pista"
    };

    public ModificarMisionManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ModificarMisionAsync(
                It.IsAny<Mision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaConMision_LlamaModificarMisionAsyncUnaVez()
    {
        var busqueda = BusquedaConMision();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new ModificarMisionComando(busqueda.Id, DtoValido()),
            CancellationToken.None);

        _repositorio.Verify(
            r => r.ModificarMisionAsync(It.IsAny<Mision>(), It.IsAny<CancellationToken>()),
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
            new ModificarMisionComando(busquedaId, DtoValido()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda sin misión", "Descripción", Guid.NewGuid(), FechaFija);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new ModificarMisionComando(busqueda.Id, DtoValido()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }
}
