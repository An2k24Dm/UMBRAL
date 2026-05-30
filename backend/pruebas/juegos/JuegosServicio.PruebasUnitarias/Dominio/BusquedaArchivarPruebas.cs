using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Pruebas de BusquedaTesoro.Archivar.
public class BusquedaArchivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaActiva()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        busqueda.AgregarMisionAEtapa(etapa.Id, "Misión 1", "Desc", TipoMision.PistaTexto, "pista");
        busqueda.Activar();
        return busqueda;
    }

    [Fact]
    public void Archivar_BusquedaActiva_CambiaEstadoAArchivada()
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
