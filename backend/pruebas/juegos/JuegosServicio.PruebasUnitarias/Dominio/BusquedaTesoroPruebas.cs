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
    public void Crear_ConDatosValidos_RetornaEstadoInactiva()
    {
        var busqueda = BusquedaValida();
        busqueda.Estado.Should().Be(EstadoBusqueda.Inactiva);
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
    public void Crear_ConDatosValidos_MisionEsNula()
    {
        var busqueda = BusquedaValida();
        busqueda.Mision.Should().BeNull();
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

    [Fact]
    public void AsignarMision_EnEstadoInactiva_AsignaMisionCorrectamente()
    {
        var busqueda = BusquedaValida();
        busqueda.AsignarMision("Busca el cofre", "Encuéntralo en el parque", TipoMision.PalabraClave, "cofre_norte");
        busqueda.Mision.Should().NotBeNull();
        busqueda.Mision!.Titulo.Should().Be("Busca el cofre");
    }

    [Fact]
    public void AsignarMision_DosVeces_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaValida();
        busqueda.AsignarMision("Misión 1", "Desc", TipoMision.PalabraClave, "clave");
        Action accion = () => busqueda.AsignarMision("Misión 2", "Desc", TipoMision.PalabraClave, "clave2");
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void EliminarMision_ConMisionAsignada_DejaLaMisionNula()
    {
        var busqueda = BusquedaValida();
        busqueda.AsignarMision("Misión", "Desc", TipoMision.PalabraClave, "clave");
        busqueda.EliminarMision();
        busqueda.Mision.Should().BeNull();
    }

    [Fact]
    public void EliminarMision_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaValida();
        Action accion = () => busqueda.EliminarMision();
        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void AgregarPistaAMision_ConMisionAsignada_AgregaLaPista()
    {
        var busqueda = BusquedaValida();
        busqueda.AsignarMision("Misión", "Desc", TipoMision.PalabraClave, "clave");
        busqueda.AgregarPistaAMision("Busca cerca del lago");
        busqueda.Mision!.Pistas.Should().ContainSingle(p => p.Contenido == "Busca cerca del lago");
    }

    [Fact]
    public void AgregarPistaAMision_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaValida();
        Action accion = () => busqueda.AgregarPistaAMision("Una pista");
        accion.Should().Throw<ExcepcionNoEncontrado>();
    }
}
