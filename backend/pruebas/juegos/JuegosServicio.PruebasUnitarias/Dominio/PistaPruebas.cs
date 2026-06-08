using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Pruebas de BusquedaTesoro.AgregarPista y la entidad Pista.
public class PistaPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaInactiva() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    [Fact]
    public void AgregarPista_ConContenidoValido_RetornaPistaConIdNoVacio()
    {
        var busqueda = BusquedaInactiva();

        var pista = busqueda.AgregarPista("Busca el árbol más alto del parque.");

        pista.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarPista_ConContenidoValido_AsignaBusquedaId()
    {
        var busqueda = BusquedaInactiva();

        var pista = busqueda.AgregarPista("Pista de prueba.");

        pista.BusquedaId.Should().Be(busqueda.Id);
    }

    [Fact]
    public void AgregarPista_VariasPistas_AparecenEnListaDePistas()
    {
        var busqueda = BusquedaInactiva();

        busqueda.AgregarPista("Pista 1.");
        busqueda.AgregarPista("Pista 2.");

        busqueda.Pistas.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPista_ContenidoVacioOEspacios_LanzaExcepcionDominio(string contenido)
    {
        var busqueda = BusquedaInactiva();

        Action accion = () => busqueda.AgregarPista(contenido);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPista_BusquedaActiva_LanzaExcepcionDominio()
    {
        // Cambio de regla: una búsqueda activa no acepta pistas nuevas.
        var busqueda = BusquedaInactiva();
        busqueda.AgregarPista("Pista inicial que habilita activar.");
        busqueda.Activar();

        Action accion = () => busqueda.AgregarPista("Mira cerca de la fuente.");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPista_ConEspaciosEnContenido_NormalizaConTrim()
    {
        var busqueda = BusquedaInactiva();

        var pista = busqueda.AgregarPista("  Mira hacia el norte  ");

        pista.Contenido.Should().Be("Mira hacia el norte");
    }
}
