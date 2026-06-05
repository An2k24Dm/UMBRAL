using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class AgregarPistaManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();
    private readonly Mock<IValidador<AgregarPistaComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private AgregarPistaManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object, _validador.Object);

    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    private static AgregarPistaComando ComandoValido(Guid busquedaId) =>
        new(busquedaId, new AgregarPistaDto { Contenido = "Busca la fuente principal del parque." });

    public AgregarPistaManejadorPruebas()
    {
        _repositorioMisiones.Setup(r => r.EsContenidoUsadoEnMisionActivaAsync(
            It.IsAny<TipoModoDeJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _validador.Setup(v => v.Validar(It.IsAny<AgregarPistaComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
        _repositorio
            .Setup(r => r.AgregarPistaAsync(It.IsAny<Pista>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaExistente_RetornaIdDePistaNoVacio()
    {
        var busqueda = BusquedaInactiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var resultado = await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_BusquedaExistente_LlamaAgregarPistaAsyncUnaVez()
    {
        var busqueda = BusquedaInactiva();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(ComandoValido(busqueda.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarPistaAsync(It.IsAny<Pista>(), It.IsAny<CancellationToken>()),
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
    public async Task Handle_BusquedaInexistente_NoLlamaAgregarPistaAsync()
    {
        var busquedaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busquedaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusquedaTesoro?)null);

        try { await CrearManejador().Handle(ComandoValido(busquedaId), CancellationToken.None); }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.AgregarPistaAsync(It.IsAny<Pista>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
