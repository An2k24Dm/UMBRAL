using JuegosServicio.Aplicacion.Comandos.ModificarPista;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class ModificarPistaManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarPistaManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object, Mock.Of<IRegistroLogsAplicacion>());

    private static BusquedaTesoro BusquedaConPista(out Guid pistaId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var pista = busqueda.AgregarPista("Pista original.");
        pistaId = pista.Id;
        return busqueda;
    }

    private static ModificarPistaComando ComandoValido(Guid busquedaId, Guid pistaId) =>
        new(busquedaId, pistaId, new ModificarPistaDto { NuevoContenido = "Pista actualizada." });

    public ModificarPistaManejadorPruebas()
    {
        _repositorioMisiones.Setup(r => r.EsContenidoUsadoEnMisionActivaAsync(
            It.IsAny<TipoModoDeJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ModificarPistaAsync(It.IsAny<Pista>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TodosExistentes_LlamaModificarPistaAsyncUnaVez()
    {
        var busqueda = BusquedaConPista(out var pistaId);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(ComandoValido(busqueda.Id, pistaId), CancellationToken.None);

        _repositorio.Verify(
            r => r.ModificarPistaAsync(It.IsAny<Pista>(), It.IsAny<CancellationToken>()),
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
            .Handle(ComandoValido(busquedaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConPista(out _);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador()
            .Handle(ComandoValido(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }
}
