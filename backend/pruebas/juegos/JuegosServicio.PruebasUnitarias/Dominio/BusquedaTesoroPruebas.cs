using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

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
    public void Crear_ConDatosValidos_PistasVacias()
    {
        var busqueda = BusquedaValida();
        busqueda.Pistas.Should().BeEmpty();
    }

    [Fact]
    public void Crear_ConDatosValidos_GeneraEventoBusquedaCreada()
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
    public void Crear_ConEspaciosEnNombre_NormalizaConTrim()
    {
        var busqueda = BusquedaTesoro.Crear(
            "  Búsqueda con espacios  ", "Descripción", CreadorId, FechaFija);
        busqueda.Nombre.Should().Be("Búsqueda con espacios");
    }

    [Fact]
    public void AgregarPista_EnEstadoInactiva_AgregaLaPista()
    {
        var busqueda = BusquedaValida();
        busqueda.AgregarPista("Busca cerca del lago.", TipoPista.Texto, null, null);
        busqueda.Pistas.Should().ContainSingle(p => p.Contenido == "Busca cerca del lago.");
    }

    [Fact]
    public void AgregarPista_EnEstadoActiva_LanzaExcepcionDominio()
    {
        // Regla nueva del ERS: no se pueden agregar pistas a una
        // búsqueda activa. La invariante vive en el estado.
        var busqueda = BusquedaValida();
        busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816); // requerida para activar
        busqueda.Activar();

        Action accion = () => busqueda.AgregarPista("Otra pista que no debería entrar.", TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarPista_EnEstadoInactiva_ActualizaContenido()
    {
        var busqueda = BusquedaValida();
        var pista = busqueda.AgregarPista("Pista original.", TipoPista.Texto, null, null);

        busqueda.ModificarPista(pista.Id, "Pista actualizada.", TipoPista.Texto, null, null);

        busqueda.Pistas[0].Contenido.Should().Be("Pista actualizada.");
    }

    [Fact]
    public void ModificarPista_EnEstadoActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaValida();
        var pista = busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816); // habilita activar
        busqueda.Activar();

        Action accion = () => busqueda.ModificarPista(pista.Id, "Cambio no permitido.", TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void EliminarPista_EnEstadoInactiva_QuitaLaPista()
    {
        var busqueda = BusquedaValida();
        var pista = busqueda.AgregarPista("Pista a eliminar.", TipoPista.Texto, null, null);

        busqueda.EliminarPista(pista.Id);

        busqueda.Pistas.Should().BeEmpty();
    }

    [Fact]
    public void EliminarPista_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaValida();

        Action accion = () => busqueda.EliminarPista(Guid.NewGuid());

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }
}
