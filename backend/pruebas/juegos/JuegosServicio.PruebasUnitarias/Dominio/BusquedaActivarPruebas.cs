using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

public class BusquedaActivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    // Búsqueda Inactiva sin pistas. Para los tests que necesitan
    // activarla, hay que agregar al menos una pista antes (regla nueva).
    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    private static BusquedaTesoro BusquedaInactivaConUnaPista()
    {
        var busqueda = BusquedaInactiva();
        busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816);
        return busqueda;
    }

    [Fact]
    public void Activar_BusquedaInactivaConPistas_CambiaEstadoAActiva()
    {
        var busqueda = BusquedaInactivaConUnaPista();

        busqueda.Activar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Activa);
    }

    [Fact]
    public void Activar_BusquedaSinPistas_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaInactiva();

        Action accion = () => busqueda.Activar();

        accion.Should()
            .Throw<ExcepcionDominio>()
            .WithMessage("La búsqueda del tesoro debe tener una coordenada GPS del tesoro para poder activarse.");
    }

    [Fact]
    public void Activar_BusquedaInactivaConPistas_AgregaEventoBusquedaActivada()
    {
        var busqueda = BusquedaInactivaConUnaPista();
        busqueda.LimpiarEventos();

        busqueda.Activar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaActivadaEvento);
    }

    [Fact]
    public void Activar_BusquedaYaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaInactivaConUnaPista();
        busqueda.Activar();

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_BusquedaActiva_NoPermiteModificarPistas()
    {
        var busqueda = BusquedaInactiva();
        busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816);
        busqueda.Activar();
        var pistaId = busqueda.Pistas[0].Id;

        Action accion = () => busqueda.ModificarPista(pistaId, "nuevo contenido", TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
