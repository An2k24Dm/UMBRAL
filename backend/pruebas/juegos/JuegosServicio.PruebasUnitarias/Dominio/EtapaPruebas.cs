using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU22/HU27: pruebas de BusquedaTesoro.AgregarEtapa y la entidad Etapa.
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

        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa del recorrido", 1);

        etapa.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarEtapa_ConDatosValidos_AsignaBusquedaId()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);

        etapa.BusquedaId.Should().Be(busqueda.Id);
    }

    [Fact]
    public void AgregarEtapa_ConOrdenExplicito_AsignaOrdenIndicado()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción", 5);

        etapa.Orden.Should().Be(5);
    }

    [Fact]
    public void AgregarEtapa_VariasEtapas_OrdenesDistintos_SeGuardanCorrectamente()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa1 = busqueda.AgregarEtapa("Etapa A", "Descripción", 1);
        var etapa2 = busqueda.AgregarEtapa("Etapa B", "Descripción", 2);
        var etapa3 = busqueda.AgregarEtapa("Etapa C", "Descripción", 3);

        etapa1.Orden.Should().Be(1);
        etapa2.Orden.Should().Be(2);
        etapa3.Orden.Should().Be(3);
    }

    [Fact]
    public void AgregarEtapa_ConDatosValidos_AumentaConteoDeEtapas()
    {
        var busqueda = BusquedaEnBorrador();

        busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        busqueda.AgregarEtapa("Etapa 2", "Descripción", 2);

        busqueda.Etapas.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarEtapa_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaEnBorrador();

        Action accion = () => busqueda.AgregarEtapa(titulo, "Descripción", 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarEtapa_DescripcionVaciaOEspacios_LanzaExcepcionDominio(string descripcion)
    {
        var busqueda = BusquedaEnBorrador();

        Action accion = () => busqueda.AgregarEtapa("Título válido", descripcion, 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarEtapa_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda archivada", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Activa, FechaFija,
            Enumerable.Empty<Etapa>());

        Action accion = () => busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    // HU27 — validaciones de orden
    [Fact]
    public void AgregarEtapa_OrdenDuplicado_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaEnBorrador();
        busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);

        Action accion = () => busqueda.AgregarEtapa("Etapa 2", "Otra descripción", 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AgregarEtapa_OrdenMenorOIgualACero_LanzaExcepcionDominio(int orden)
    {
        var busqueda = BusquedaEnBorrador();

        Action accion = () => busqueda.AgregarEtapa("Etapa 1", "Descripción", orden);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarEtapa_OrdenNoConsecutivo_SePermite()
    {
        var busqueda = BusquedaEnBorrador();

        var etapa1 = busqueda.AgregarEtapa("Etapa 1", "Descripción", 1);
        var etapa3 = busqueda.AgregarEtapa("Etapa 3", "Descripción", 3);

        etapa1.Orden.Should().Be(1);
        etapa3.Orden.Should().Be(3);
        busqueda.Etapas.Should().HaveCount(2);
    }
}
