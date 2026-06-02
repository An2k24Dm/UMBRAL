using JuegosServicio.Dominio.Enums;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU28: pruebas del manejador para agregar una pista a la misión.
public class AgregarPistaManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private AgregarPistaManejador CrearManejador() => new(_repositorio.Object);

    private static BusquedaTesoro BusquedaConMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Misión", "Desc", TipoMision.PalabraClave, "clave");
        return busqueda;
    }

    private static AgregarPistaComando ComandoValido(Guid busquedaId) =>
        new(busquedaId, new AgregarPistaDto
        {
            Contenido = "Busca la fuente principal del parque."
        });

    public AgregarPistaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.AgregarPistaAsync(
                It.IsAny<Guid>(), It.IsAny<Pista>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaConMision_RetornaIdDePistaNoVacio()
    {
        var busqueda = BusquedaConMision();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var resultado = await CrearManejador()
            .Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BusquedaConMision_LlamaAgregarPistaAsyncUnaVez()
    {
        var busqueda = BusquedaConMision();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarPistaAsync(busqueda.Mision!.Id, It.IsAny<Pista>(), It.IsAny<CancellationToken>()),
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
    public async Task Handle_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda sin misión", "Descripción", Guid.NewGuid(), FechaFija);
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () =>
            await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_BusquedaInexistente_NoLlamaAgregarPistaAsync()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        try { await CrearManejador().Handle(ComandoValido(busquedaId), CancellationToken.None); }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.AgregarPistaAsync(It.IsAny<Guid>(), It.IsAny<Pista>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
