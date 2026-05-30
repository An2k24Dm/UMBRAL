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

    private static BusquedaTesoro BusquedaConEtapaYMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        busqueda.AgregarMisionAEtapa(etapa.Id, "Misión 1", "Desc", TipoMision.PistaTexto, "pista");
        return busqueda;
    }

    [Fact]
    public void Activar_BusquedaConEtapasYMisiones_CambiaEstadoAActiva()
    {
        var busqueda = BusquedaConEtapaYMision();

        busqueda.Activar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Activa);
    }

    [Fact]
    public void Activar_BusquedaConEtapasYMisiones_AgregaEventoBusquedaActivada()
    {
        var busqueda = BusquedaConEtapaYMision();
        busqueda.LimpiarEventos();

        busqueda.Activar();

        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaActivadaEvento);
    }

    [Fact]
    public void Activar_BusquedaSinEtapas_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda vacía", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_EtapaSinMisiones_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AgregarEtapa("Etapa vacía", "Sin misiones", 1);

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_BusquedaNoEnBorrador_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaConEtapaYMision();
        busqueda.Activar();

        Action accion = () => busqueda.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_VariasEtapasConMisiones_CambiaEstadoAActiva()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa1 = busqueda.AgregarEtapa("Etapa 1", "Desc", 1);
        busqueda.AgregarMisionAEtapa(etapa1.Id, "M1", "Desc", TipoMision.Acertijo, "pista1");
        var etapa2 = busqueda.AgregarEtapa("Etapa 2", "Desc", 2);
        busqueda.AgregarMisionAEtapa(etapa2.Id, "M2", "Desc", TipoMision.CodigoQR, "pista2");

        busqueda.Activar();

        busqueda.Estado.Should().Be(EstadoBusqueda.Activa);
    }
}
