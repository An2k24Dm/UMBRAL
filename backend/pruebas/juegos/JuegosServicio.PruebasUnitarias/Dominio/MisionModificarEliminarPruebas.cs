using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU25: pruebas de BusquedaTesoro.ModificarMision y EliminarMision.
public class MisionModificarEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Misión original", "Desc original", TipoMision.PalabraClave, "pista-original");
        return busqueda;
    }

    // --- ModificarMision ---

    [Fact]
    public void ModificarMision_ConDatosValidos_ActualizaLosCampos()
    {
        var busqueda = BusquedaConMision();

        busqueda.ModificarMision("Nuevo título", "Nueva desc", TipoMision.PalabraClave, "nueva-pista");

        busqueda.Mision!.Titulo.Should().Be("Nuevo título");
        busqueda.Mision.Descripcion.Should().Be("Nueva desc");
        busqueda.Mision.PistaClave.Should().Be("nueva-pista");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarMision_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaConMision();

        Action accion = () => busqueda.ModificarMision(titulo, "Desc", TipoMision.PalabraClave, "pista");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarMision_PistaClaveVaciaOEspacios_LanzaExcepcionDominio(string pistaClave)
    {
        var busqueda = BusquedaConMision();

        Action accion = () => busqueda.ModificarMision("Título", "Desc", TipoMision.PalabraClave, pistaClave);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarMision_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda sin misión", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.ModificarMision("Título", "Desc", TipoMision.PalabraClave, "pista");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    // --- EliminarMision ---

    [Fact]
    public void EliminarMision_ConMisionAsignada_DejaLaMisionNula()
    {
        var busqueda = BusquedaConMision();

        busqueda.EliminarMision();

        busqueda.Mision.Should().BeNull();
    }

    [Fact]
    public void EliminarMision_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda sin misión", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.EliminarMision();

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void EliminarMision_BusquedaActiva_LanzaExcepcionDominio()
    {
        var mision = Mision.Reconstituir(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Desc", TipoMision.PalabraClave, "pista");
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Activa, FechaFija,
            mision);

        Action accion = () => busqueda.EliminarMision();

        accion.Should().Throw<ExcepcionDominio>();
    }
}
