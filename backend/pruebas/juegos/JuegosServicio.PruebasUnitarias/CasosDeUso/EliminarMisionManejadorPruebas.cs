using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU25: pruebas del manejador de eliminación de misión.
public class EliminarMisionManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private EliminarMisionManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConMision(out Guid etapaId, out Guid misionId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        etapaId = etapa.Id;
        var mision = busqueda.AgregarMisionAEtapa(etapaId, "Misión", "Desc", TipoMision.PistaTexto, "pista");
        misionId = mision.Id;
        return busqueda;
    }

    public EliminarMisionManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarMisionAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TodosExistentes_LlamaEliminarMisionAsyncUnaVez()
    {
        var busqueda = BusquedaConMision(out var etapaId, out var misionId);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new EliminarMisionComando(busqueda.Id, etapaId, misionId), CancellationToken.None);

        _repositorio.Verify(
            r => r.EliminarMisionAsync(etapaId, misionId, It.IsAny<CancellationToken>()),
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
            new EliminarMisionComando(busquedaId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_MisionInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConMision(out var etapaId, out _);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new EliminarMisionComando(busqueda.Id, etapaId, Guid.NewGuid()),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }
}
