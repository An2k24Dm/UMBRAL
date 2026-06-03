using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

public class BusquedaActivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    [Fact]
    public void Activar_BusquedaInactiva_CambiaEstadoAActiva()
    {
        var busqueda = BusquedaInactiva();

        busqueda.Activar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Activa);
    }

    [Fact]
    public void Activar_BusquedaInactiva_AgregaEventoBusquedaActivada()
    {
        var busqueda = BusquedaInactiva();
        busqueda.LimpiarEventos();

        busqueda.Activar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaActivadaEvento);
    }

    [Fact]
    public void Activar_BusquedaYaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaInactiva();
        busqueda.Activar();

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_BusquedaActiva_NoPermiteModificarPistas()
    {
        var busqueda = BusquedaInactiva();
        busqueda.AgregarPista("pista de ayuda");
        busqueda.Activar();
        var pistaId = busqueda.Pistas[0].Id;

        Action accion = () => busqueda.ModificarPista(pistaId, "nuevo contenido");

        accion.Should().Throw<ExcepcionDominio>();
    }
}
