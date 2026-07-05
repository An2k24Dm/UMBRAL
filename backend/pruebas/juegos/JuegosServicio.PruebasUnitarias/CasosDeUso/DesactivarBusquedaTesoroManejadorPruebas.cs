using JuegosServicio.Aplicacion.Comandos.DesactivarBusquedaTesoro;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// Pruebas del manejador de desactivación.
//
// El manejador ya NO consulta sesiones-servicio: el endpoint viejo
// `/contenidos/{tipo}/{id}/existe-vigente` fue eliminado al cambiar el
// modelo a Sesion → Mision → Etapa. La protección contra contenido en
// uso vive ahora en el dominio (no se modifica/agrega/elimina si está
// activo) y en el manejador de Eliminar.
public class DesactivarBusquedaTesoroManejadorPruebas
{
    private readonly Mock<IRepositorioBusquedas> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private DesactivarBusquedaTesoroManejador CrearManejador() =>
        new(_repositorio.Object, Mock.Of<IRegistroLogsAplicacion>());

    private static BusquedaTesoro BusquedaActivaConUnaPista()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        // La regla nueva exige al menos una pista antes de activar; de
        // lo contrario `Activar` lanza ExcepcionDominio.
        busqueda.AgregarPista("Pista única");
        busqueda.Activar();
        return busqueda;
    }

    public DesactivarBusquedaTesoroManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.DesactivarBusquedaTesoroAsync(
                It.IsAny<BusquedaTesoro>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_BusquedaActiva_DesactivaYPersiste()
    {
        var busqueda = BusquedaActivaConUnaPista();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        await CrearManejador().Handle(
            new DesactivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        busqueda.Estado.Should().Be(EstadoBusqueda.Inactiva);
        _repositorio.Verify(
            r => r.DesactivarBusquedaTesoroAsync(busqueda, It.IsAny<CancellationToken>()),
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
            new DesactivarBusquedaTesoroComando(busquedaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_BusquedaYaInactiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaActivaConUnaPista();
        busqueda.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerBusquedaPorIdAsync(busqueda.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(busqueda);

        var accion = async () => await CrearManejador().Handle(
            new DesactivarBusquedaTesoroComando(busqueda.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
