using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU30: pruebas de BusquedaTesoro.ModificarPista.
public class PistaModificarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConEtapaYPista(out Guid etapaId, out Guid pistaId)
    {
        var busqueda = BusquedaTesoro.Crear(
            "Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa");
        etapaId = etapa.Id;
        var pista = busqueda.AgregarPistaAEtapa(etapaId, "Pista original.");
        pistaId = pista.Id;
        return busqueda;
    }

    [Fact]
    public void ModificarPista_ConContenidoValido_ActualizaContenido()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);

        busqueda.ModificarPista(etapaId, pistaId, "Nuevo contenido de pista.");

        var pista = busqueda.Etapas.First(e => e.Id == etapaId).Pistas.First(p => p.Id == pistaId);
        pista.Contenido.Should().Be("Nuevo contenido de pista.");
    }

    [Fact]
    public void ModificarPista_ConEspacios_NormalizaConTrim()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);

        busqueda.ModificarPista(etapaId, pistaId, "  Contenido con espacios  ");

        var pista = busqueda.Etapas.First(e => e.Id == etapaId).Pistas.First(p => p.Id == pistaId);
        pista.Contenido.Should().Be("Contenido con espacios");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarPista_ContenidoVacioOEspacios_LanzaExcepcionDominio(string contenido)
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);

        Action accion = () => busqueda.ModificarPista(etapaId, pistaId, contenido);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarPista_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapaYPista(out _, out var pistaId);

        Action accion = () => busqueda.ModificarPista(Guid.NewGuid(), pistaId, "Nuevo contenido.");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void ModificarPista_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out _);

        Action accion = () => busqueda.ModificarPista(etapaId, Guid.NewGuid(), "Nuevo contenido.");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void ModificarPista_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);
        busqueda.AgregarMisionAEtapa(etapaId, "Misión", "Desc", TipoMision.PistaTexto, "clave");
        busqueda.Activar();

        Action accion = () => busqueda.ModificarPista(etapaId, pistaId, "Nuevo contenido.");

        accion.Should().Throw<ExcepcionDominio>();
    }
}
