using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Pruebas de BusquedaTesoro.Desactivar.
public class BusquedaArchivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaActiva()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Busca el cofre", "Encuéntralo en el parque", TipoMision.PalabraClave, "cofre_norte");
        busqueda.Activar();
        return busqueda;
    }

    [Fact]
    public void Archivar_BusquedaActiva_CambiaEstadoAInactiva()
    {
        var busqueda = BusquedaActiva();

        busqueda.Desactivar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Inactiva);
    }

    [Fact]
    public void Archivar_BusquedaYaInactiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Inactiva", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Archivar_BusquedaActiva_AgregaEventoBusquedaArchivada()
    {
        var busqueda = BusquedaActiva();
        busqueda.LimpiarEventos();

        busqueda.Desactivar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaArchivadaEvento);
    }

    [Fact]
    public void Archivar_BusquedaYaArchivada_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaActiva();
        busqueda.Desactivar();

        Action accion = () => busqueda.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }
}
