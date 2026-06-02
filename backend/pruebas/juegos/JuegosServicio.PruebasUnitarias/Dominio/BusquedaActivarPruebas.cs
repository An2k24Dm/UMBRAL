using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU26: pruebas de BusquedaTesoro.Activar.
public class BusquedaActivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Busca el cofre", "Encuéntralo en el parque", TipoMision.PalabraClave, "cofre_norte");
        return busqueda;
    }

    [Fact]
    public void Activar_BusquedaConMision_CambiaEstadoAActiva()
    {
        var busqueda = BusquedaConMision();

        busqueda.Activar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Activa);
    }

    [Fact]
    public void Activar_BusquedaConMision_AgregaEventoBusquedaActivada()
    {
        var busqueda = BusquedaConMision();
        busqueda.LimpiarEventos();

        busqueda.Activar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaActivadaEvento);
    }

    [Fact]
    public void Activar_BusquedaSinMision_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda vacía", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_BusquedaYaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaConMision();
        busqueda.Activar();

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_BusquedaActiva_NoPermiteAsignarMision()
    {
        var busqueda = BusquedaConMision();
        busqueda.Activar();

        Action accion = () => busqueda.AsignarMision("Otra misión", "Desc", TipoMision.PalabraClave, "clave");

        accion.Should().Throw<ExcepcionDominio>();
    }
}
