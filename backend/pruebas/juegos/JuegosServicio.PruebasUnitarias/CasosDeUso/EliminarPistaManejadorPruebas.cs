using JuegosServicio.Dominio.Enums;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU32: pruebas del manejador para eliminar una pista.
public class EliminarPistaManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private EliminarPistaManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConPista(out Guid pistaId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Misión", "Desc", TipoMision.PalabraClave, "pista-clave");
        var pista = busqueda.AgregarPistaAMision("Pista de prueba.");
        pistaId = pista.Id;
        return busqueda;
    }

    public EliminarPistaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarPistaAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TodosExistentes_LlamaEliminarPistaAsyncUnaVez()
    {
        var busqueda = BusquedaConPista(out var pistaId);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new EliminarPistaComando(busqueda.Id, pistaId), CancellationToken.None);

        _repositorio.Verify(
            r => r.EliminarPistaAsync(pistaId, It.IsAny<CancellationToken>()),
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
            new EliminarPistaComando(busquedaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConPista(out _);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new EliminarPistaComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_NoLlamaEliminarPistaAsync()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        try { await CrearManejador().Handle(
            new EliminarPistaComando(busquedaId, Guid.NewGuid()), CancellationToken.None); }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.EliminarPistaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
