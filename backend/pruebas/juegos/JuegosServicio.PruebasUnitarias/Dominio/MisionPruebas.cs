using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU23: pruebas de BusquedaTesoro.AsignarMision y la entidad Mision.
public class MisionPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaValida() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    [Fact]
    public void AsignarMision_ConDatosValidos_RetornaMisionConIdNoVacio()
    {
        var busqueda = BusquedaValida();

        var mision = busqueda.AsignarMision("Busca el cofre", "Descripción", TipoMision.PalabraClave, "cofre_norte");

        mision.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AsignarMision_ConDatosValidos_AsignaBusquedaId()
    {
        var busqueda = BusquedaValida();

        var mision = busqueda.AsignarMision("Busca el cofre", "Descripción", TipoMision.PalabraClave, "cofre_norte");

        mision.BusquedaId.Should().Be(busqueda.Id);
    }

    [Fact]
    public void AsignarMision_ConDatosValidos_GuardaMisionEnLaBusqueda()
    {
        var busqueda = BusquedaValida();

        busqueda.AsignarMision("Busca el cofre", "Descripción", TipoMision.PalabraClave, "cofre_norte");

        busqueda.Mision.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AsignarMision_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaValida();

        Action accion = () => busqueda.AsignarMision(titulo, "Descripción", TipoMision.PalabraClave, "Pista");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AsignarMision_PistaClaveVaciaOEspacios_LanzaExcepcionDominio(string pistaClave)
    {
        var busqueda = BusquedaValida();

        Action accion = () => busqueda.AsignarMision("Título", "Descripción", TipoMision.PalabraClave, pistaClave);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AsignarMision_BusquedaActiva_LanzaExcepcionDominio()
    {
        var mision = Mision.Reconstituir(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Desc", TipoMision.PalabraClave, "pista");
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda Activa", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Activa, FechaFija,
            mision);

        Action accion = () => busqueda.AsignarMision("Otra misión", "Desc", TipoMision.PalabraClave, "clave");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AsignarMision_YaTieneMision_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaValida();
        busqueda.AsignarMision("Primera misión", "Desc", TipoMision.PalabraClave, "clave1");

        Action accion = () => busqueda.AsignarMision("Segunda misión", "Desc", TipoMision.PalabraClave, "clave2");

        accion.Should().Throw<ExcepcionDominio>();
    }
}
