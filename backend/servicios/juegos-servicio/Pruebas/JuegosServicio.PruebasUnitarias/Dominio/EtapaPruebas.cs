using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU22: pruebas de BusquedaTesoro.AgregarEtapa y la entidad Etapa.
public class EtapaPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaEnBorrador() =>
        BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);

    [Fact]
    public void AgregarEtapa_ConDatosValidos_RetornaEtapaConIdNoVacio()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa del recorrido");

        etapa.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarEtapa_ConDatosValidos_AsignaBusquedaId()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción");

        etapa.BusquedaId.Should().Be(busqueda.Id);
    }

    [Fact]
    public void AgregarEtapa_PrimeraEtapa_AsignaOrden1()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción");

        etapa.Orden.Should().Be(1);
    }

    [Fact]
    public void AgregarEtapa_VariasEtapas_AsignaOrdenConsecutivo()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa1 = busqueda.AgregarEtapa("Etapa 1", "Descripción");
        var etapa2 = busqueda.AgregarEtapa("Etapa 2", "Descripción");
        var etapa3 = busqueda.AgregarEtapa("Etapa 3", "Descripción");

        etapa1.Orden.Should().Be(1);
        etapa2.Orden.Should().Be(2);
        etapa3.Orden.Should().Be(3);
    }

    [Fact]
    public void AgregarEtapa_ConDatosValidos_AumentaConteoDeEtapas()
    {
        var busqueda = BusquedaEnBorrador();

        busqueda.AgregarEtapa("Etapa 1", "Descripción");
        busqueda.AgregarEtapa("Etapa 2", "Descripción");

        busqueda.Etapas.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarEtapa_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaEnBorrador();

        Action accion = () => busqueda.AgregarEtapa(titulo, "Descripción");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarEtapa_DescripcionVaciaOEspacios_LanzaExcepcionDominio(string descripcion)
    {
        var busqueda = BusquedaEnBorrador();

        Action accion = () => busqueda.AgregarEtapa("Título válido", descripcion);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarEtapa_BusquedaArchivada_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda archivada", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Archivada, FechaFija,
            Enumerable.Empty<Etapa>());

        Action accion = () => busqueda.AgregarEtapa("Etapa 1", "Descripción");

        accion.Should().Throw<ExcepcionDominio>();
    }
}
