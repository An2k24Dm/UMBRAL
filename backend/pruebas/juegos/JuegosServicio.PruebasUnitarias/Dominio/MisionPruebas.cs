using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU23: pruebas de BusquedaTesoro.AgregarMisionAEtapa y la entidad Mision.
public class MisionPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConEtapa(out Guid etapaId)
    {
        var busqueda = BusquedaTesoro.Crear(
            "Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa", 1);
        etapaId = etapa.Id;
        return busqueda;
    }

    [Fact]
    public void AgregarMisionAEtapa_ConDatosValidos_RetornaMisionConIdNoVacio()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        var mision = busqueda.AgregarMisionAEtapa(
            etapaId, "Misión 1", "Descripción", TipoMision.PistaTexto, "Busca el árbol");

        mision.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarMisionAEtapa_ConDatosValidos_AsignaEtapaId()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        var mision = busqueda.AgregarMisionAEtapa(
            etapaId, "Misión 1", "Descripción", TipoMision.Acertijo, "¿Qué tiene alas?");

        mision.EtapaId.Should().Be(etapaId);
    }

    [Fact]
    public void AgregarMisionAEtapa_ConDatosValidos_AsignaTipoMision()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        var mision = busqueda.AgregarMisionAEtapa(
            etapaId, "Misión QR", "Descripción", TipoMision.CodigoQR, "QR-001");

        mision.Tipo.Should().Be(TipoMision.CodigoQR);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarMisionAEtapa_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        Action accion = () => busqueda.AgregarMisionAEtapa(
            etapaId, titulo, "Descripción", TipoMision.PistaTexto, "Pista");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarMisionAEtapa_PistaClaVaciaOEspacios_LanzaExcepcionDominio(string pistaClave)
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        Action accion = () => busqueda.AgregarMisionAEtapa(
            etapaId, "Título", "Descripción", TipoMision.PistaTexto, pistaClave);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarMisionAEtapa_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapa(out _);
        var etapaIdInexistente = Guid.NewGuid();

        Action accion = () => busqueda.AgregarMisionAEtapa(
            etapaIdInexistente, "Título", "Descripción", TipoMision.Acertijo, "Pista");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void AgregarMisionAEtapa_BusquedaActiva_LanzaExcepcionDominio()
    {
        var etapaId = Guid.NewGuid();
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda archivada", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Activa, FechaFija,
            new[] { Etapa.Reconstituir(etapaId, Guid.NewGuid(), "Etapa", "Desc", 1, []) });

        Action accion = () => busqueda.AgregarMisionAEtapa(
            etapaId, "Título", "Descripción", TipoMision.PistaTexto, "Pista");

        accion.Should().Throw<ExcepcionDominio>();
    }
}
