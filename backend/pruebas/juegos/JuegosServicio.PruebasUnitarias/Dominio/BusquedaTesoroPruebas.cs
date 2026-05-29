using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU21: pruebas del aggregate root BusquedaTesoro — creación y estado inicial.
public class BusquedaTesoroPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaValida() => BusquedaTesoro.Crear(
        "Búsqueda del Parque Central",
        "Recorre el parque resolviendo acertijos",
        CreadorId,
        FechaFija);

    [Fact]
    public void Crear_ConDatosValidos_RetornaEstadoBorrador()
    {
        var busqueda = BusquedaValida();
        busqueda.Estado.Should().Be(EstadoBusqueda.Borrador);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaIdNoVacio()
    {
        var busqueda = BusquedaValida();
        busqueda.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaCreadorId()
    {
        var busqueda = BusquedaValida();
        busqueda.CreadorId.Should().Be(CreadorId);
    }

    [Fact]
    public void Crear_ConDatosValidos_AsignaFechaCreacion()
    {
        var busqueda = BusquedaValida();
        busqueda.FechaCreacion.Should().Be(FechaFija);
    }

    [Fact]
    public void Crear_ConEspaciosEnNombre_NormalizaConTrim()
    {
        var busqueda = BusquedaTesoro.Crear(
            "  Búsqueda con espacios  ", "Descripción", CreadorId, FechaFija);

        busqueda.Nombre.Should().Be("Búsqueda con espacios");
    }

    [Fact]
    public void Crear_ConDatosValidos_ListaDeEtapasVacia()
    {
        var busqueda = BusquedaValida();
        busqueda.Etapas.Should().BeEmpty();
    }

    [Fact]
    public void Crear_ConDatosValidos_GeneraEventoBusquedaCreadaEnMemoria()
    {
        var busqueda = BusquedaValida();
        busqueda.Eventos.Should().ContainSingle(e => e is BusquedaCreadaEvento);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_NombreVacioOEspacios_LanzaExcepcionDominio(string nombre)
    {
        Action accion = () =>
            BusquedaTesoro.Crear(nombre, "Descripción", CreadorId, FechaFija);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_DescripcionVaciaOEspacios_LanzaExcepcionDominio(string descripcion)
    {
        Action accion = () =>
            BusquedaTesoro.Crear("Nombre", descripcion, CreadorId, FechaFija);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Crear_CreadorIdVacio_LanzaExcepcionDominio()
    {
        Action accion = () =>
            BusquedaTesoro.Crear("Nombre", "Descripción", Guid.Empty, FechaFija);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
