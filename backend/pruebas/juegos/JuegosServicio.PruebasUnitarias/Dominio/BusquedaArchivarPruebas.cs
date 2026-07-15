using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

public class BusquedaArchivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaActiva()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        // Regla nueva: una búsqueda solo puede activarse si tiene al
        // menos una pista. Las pruebas que necesitan una búsqueda
        // activa parten de ese fixture.
        busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816);
        busqueda.Activar();
        return busqueda;
    }

    [Fact]
    public void Desactivar_BusquedaActiva_CambiaEstadoAInactiva()
    {
        var busqueda = BusquedaActiva();

        busqueda.Desactivar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Inactiva);
    }

    [Fact]
    public void Desactivar_BusquedaYaInactiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Inactiva", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Desactivar_BusquedaActiva_AgregaEventoBusquedaArchivada()
    {
        var busqueda = BusquedaActiva();
        busqueda.LimpiarEventos();

        busqueda.Desactivar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaArchivadaEvento);
    }

    [Fact]
    public void Desactivar_BusquedaYaInactivaDespuesDeDesactivar_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaActiva();
        busqueda.Desactivar();

        Action accion = () => busqueda.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }
}
