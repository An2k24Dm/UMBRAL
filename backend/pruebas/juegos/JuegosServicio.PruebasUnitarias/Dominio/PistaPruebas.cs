using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU28: pruebas del aggregate root BusquedaTesoro.AgregarPistaAEtapa y la entidad Pista.
public class PistaPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConEtapa(out Guid etapaId)
    {
        var busqueda = BusquedaTesoro.Crear(
            "Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa");
        etapaId = etapa.Id;
        return busqueda;
    }

    [Fact]
    public void AgregarPistaAEtapa_ConContenidoValido_RetornaPistaConIdNoVacio()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        var pista = busqueda.AgregarPistaAEtapa(etapaId, "Busca el árbol más alto del parque.");

        pista.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AgregarPistaAEtapa_ConContenidoValido_AsignaEtapaId()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        var pista = busqueda.AgregarPistaAEtapa(etapaId, "Pista de prueba.");

        pista.EtapaId.Should().Be(etapaId);
    }

    [Fact]
    public void AgregarPistaAEtapa_ConContenidoValido_AparecerEnListaDePistas()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        busqueda.AgregarPistaAEtapa(etapaId, "Pista 1.");
        busqueda.AgregarPistaAEtapa(etapaId, "Pista 2.");

        var etapa = busqueda.Etapas.First(e => e.Id == etapaId);
        etapa.Pistas.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AgregarPistaAEtapa_ContenidoVacioOEspacios_LanzaExcepcionDominio(string contenido)
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        Action accion = () => busqueda.AgregarPistaAEtapa(etapaId, contenido);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void AgregarPistaAEtapa_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapa(out _);

        Action accion = () => busqueda.AgregarPistaAEtapa(Guid.NewGuid(), "Pista válida.");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void AgregarPistaAEtapa_BusquedaActiva_PermiteAgregarPista()
    {
        // Las pistas son ayudas en tiempo real: se pueden agregar aunque
        // la búsqueda esté activa y haya una sesión en curso.
        var busqueda = BusquedaConEtapa(out var etapaId);
        busqueda.AgregarMisionAEtapa(etapaId, "Misión", "Desc", TipoMision.PistaTexto, "pista");
        busqueda.Activar();

        var pista = busqueda.AgregarPistaAEtapa(etapaId, "Mira cerca de la fuente.");

        pista.Id.Should().NotBe(Guid.Empty);
        busqueda.Etapas.First(e => e.Id == etapaId).Pistas.Should().HaveCount(1);
    }

    [Fact]
    public void AgregarPistaAEtapa_ConEspaciosEnContenido_NormalizaConTrim()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        var pista = busqueda.AgregarPistaAEtapa(etapaId, "  Mira hacia el norte  ");

        pista.Contenido.Should().Be("Mira hacia el norte");
    }
}
