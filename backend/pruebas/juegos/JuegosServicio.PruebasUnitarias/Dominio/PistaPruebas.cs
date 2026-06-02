using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU28: pruebas de BusquedaTesoro.AgregarPistaAMision y la entidad Pista.
public class PistaPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConMision()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Busca el cofre", "Primera misión", TipoMision.PalabraClave, "cofre_norte");
        return busqueda;
    }

    [Fact]
    public void AgregarPistaAMision_ConContenidoValido_RetornaPistaConIdNoVacio()
    {
        var busqueda = BusquedaConMision();

        var pista = busqueda.AgregarPistaAMision("Busca el árbol más alto del parque.");

        pista.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarPistaAMision_ConContenidoValido_AsignaMisionId()
    {
        var busqueda = BusquedaConMision();

        var pista = busqueda.AgregarPistaAMision("Pista de prueba.");

        pista.MisionId.Should().Be(busqueda.Mision!.Id);
    }

    [Fact]
    public void AgregarPistaAMision_VariosPistas_AparecenEnListaDePistas()
    {
        var busqueda = BusquedaConMision();

        busqueda.AgregarPistaAMision("Pista 1.");
        busqueda.AgregarPistaAMision("Pista 2.");

        busqueda.Mision!.Pistas.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPistaAMision_ContenidoVacioOEspacios_LanzaExcepcionDominio(string contenido)
    {
        var busqueda = BusquedaConMision();

        Action accion = () => busqueda.AgregarPistaAMision(contenido);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPistaAMision_SinMisionAsignada_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda sin misión", "Descripción", Guid.NewGuid(), FechaFija);

        Action accion = () => busqueda.AgregarPistaAMision("Una pista.");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void AgregarPistaAMision_BusquedaActiva_PermiteAgregarPista()
    {
        // Las pistas son ayudas en tiempo real: se pueden agregar aunque la búsqueda esté activa.
        var busqueda = BusquedaConMision();
        busqueda.Activar();

        var pista = busqueda.AgregarPistaAMision("Mira cerca de la fuente.");

        pista.Id.Should().NotBe(Guid.Empty);
        busqueda.Mision!.Pistas.Should().HaveCount(1);
    }

    [Fact]
    public void AgregarPistaAMision_ConEspaciosEnContenido_NormalizaConTrim()
    {
        var busqueda = BusquedaConMision();

        var pista = busqueda.AgregarPistaAMision("  Mira hacia el norte  ");

        pista.Contenido.Should().Be("Mira hacia el norte");
    }
}
