using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
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

        var pista = busqueda.AgregarPista("Busca el árbol más alto del parque.", TipoPista.Texto, null, null);

        pista.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarPista_ConContenidoValido_AsignaBusquedaId()
    {
        var busqueda = BusquedaInactiva();

        var pista = busqueda.AgregarPista("Pista de prueba.", TipoPista.Texto, null, null);

        pista.BusquedaId.Should().Be(busqueda.Id);
    }

    [Fact]
    public void AgregarPista_VariasPistas_AparecenEnListaDePistas()
    {
        var busqueda = BusquedaInactiva();

        busqueda.AgregarPista("Pista 1.", TipoPista.Texto, null, null);
        busqueda.AgregarPista("Pista 2.", TipoPista.Texto, null, null);

        busqueda.Pistas.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPista_ContenidoVacioOEspacios_LanzaExcepcionDominio(string contenido)
    {
        var busqueda = BusquedaInactiva();

        Action accion = () => busqueda.AgregarPista(contenido, TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPista_BusquedaActiva_LanzaExcepcionDominio()
    {
        // Cambio de regla: una búsqueda activa no acepta pistas nuevas.
        var busqueda = BusquedaInactiva();
        busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816);
        busqueda.Activar();

        Action accion = () => busqueda.AgregarPista("Mira cerca de la fuente.", TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPista_ConEspaciosEnContenido_NormalizaConTrim()
    {
        var busqueda = BusquedaInactiva();

        var pista = busqueda.AgregarPista("  Mira hacia el norte  ", TipoPista.Texto, null, null);

        pista.Contenido.Should().Be("Mira hacia el norte");
    }
}
