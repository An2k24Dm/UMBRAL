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
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción");
        busqueda.AgregarMisionAEtapa(etapa.Id, "Misión 1", "Desc", TipoMision.PistaTexto, "pista");
        busqueda.Activar();
        return busqueda;
    }

    [Fact]
    public void Archivar_BusquedaActiva_CambiaEstadoAArchivada()
    {
        var busqueda = BusquedaActiva();

        busqueda.Archivar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Archivada);
    }

    [Fact]
    public void Archivar_BusquedaEnBorrador_CambiaEstadoAArchivada()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Borrador", "Descripción", Guid.NewGuid(), FechaFija);

        busqueda.Archivar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Archivada);
    }

    [Fact]
    public void Archivar_BusquedaActiva_AgregaEventoBusquedaArchivada()
    {
        var busqueda = BusquedaActiva();
        busqueda.LimpiarEventos();

        busqueda.Archivar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaArchivadaEvento);
    }

    [Fact]
    public void Archivar_BusquedaYaArchivada_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaActiva();
        busqueda.Archivar();

        Action accion = () => busqueda.Archivar();

        accion.Should().Throw<ExcepcionDominio>();
    }
}
