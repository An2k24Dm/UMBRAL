using JuegosServicio.Aplicacion.Comandos.CrearBusquedaTesoro;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU21: pruebas del manejador de creación de búsqueda del tesoro.
public class CrearBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private readonly Mock<ILogger<CrearBusquedaTesoroManejador>> _registro = new();
    private readonly Mock<IValidador<CrearBusquedaTesoroComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private CrearBusquedaTesoroManejador CrearManejador() =>
        new(_repositorio.Object, _reloj.Object, _validador.Object, _registro.Object);

    private CrearBusquedaTesoroComando ComandoValido(string nombre = "Búsqueda del Parque") =>
        new(new CrearBusquedaTesoroDto
        {
            Nombre = nombre,
            Descripcion = "Recorre el parque resolviendo acertijos"
        }, Guid.NewGuid());

    public CrearBusquedaTesoroManejadorPruebas()
    {
        _validador.Setup(v => v.Validar(It.IsAny<CrearBusquedaTesoroComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(FechaFija);
        _repositorio
            .Setup(r => r.ExisteBusquedaConNombreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.CrearBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_NombreNuevo_RetornaIdNoVacio()
    {
        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NombreNuevo_LlamaCrearBusquedaTesoroAsyncUnaVez()
    {
        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _repositorio.Verify(
            r => r.CrearBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NombreNuevo_UsaCreadorIdDelComando()
    {
        var creadorId = Guid.NewGuid();
        var comando = new CrearBusquedaTesoroComando(
            new CrearBusquedaTesoroDto
            {
                Nombre = "Búsqueda válida",
                Descripcion = "Descripción"
            }, creadorId);

        BusquedaTesoro? busquedaGuardada = null;
        _repositorio
            .Setup(r => r.CrearBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Callback<BusquedaTesoro, CancellationToken>((b, _) => busquedaGuardada = b)
            .Returns(Task.CompletedTask);

        await CrearManejador().Handle(comando, CancellationToken.None);

        busquedaGuardada!.CreadorId.Should().Be(creadorId);
    }

    [Fact]
    public async Task Handle_NombreDuplicado_LanzaExcepcionDominio()
    {
        _repositorio
            .Setup(r => r.ExisteBusquedaConNombreAsync("Búsqueda existente", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accion = async () =>
            await CrearManejador().Handle(ComandoValido("Búsqueda existente"), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }

    [Fact]
    public async Task Handle_NombreDuplicado_NoLlamaCrearBusquedaTesoroAsync()
    {
        _repositorio
            .Setup(r => r.ExisteBusquedaConNombreAsync("Búsqueda existente", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try { await CrearManejador().Handle(ComandoValido("Búsqueda existente"), CancellationToken.None); }
        catch (ExcepcionDominio) { }

        _repositorio.Verify(
            r => r.CrearBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
