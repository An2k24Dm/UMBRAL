using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU25: pruebas de BusquedaTesoro.ModificarMision y EliminarMision.
public class MisionModificarEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConMision(out Guid etapaId, out Guid misionId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Descripción");
        etapaId = etapa.Id;
        var mision = busqueda.AgregarMisionAEtapa(etapaId, "Misión original", "Desc original", TipoMision.PistaTexto, "pista-original");
        misionId = mision.Id;
        return busqueda;
    }

    // --- ModificarMision ---

    [Fact]
    public void ModificarMision_ConDatosValidos_ActualizaLosCampos()
    {
        var busqueda = BusquedaConMision(out var etapaId, out var misionId);

        busqueda.ModificarMision(etapaId, misionId, "Nuevo título", "Nueva desc", TipoMision.Acertijo, "nueva-pista");

        var mision = busqueda.Etapas.First(e => e.Id == etapaId).Misiones.First(m => m.Id == misionId);
        mision.Titulo.Should().Be("Nuevo título");
        mision.Tipo.Should().Be(TipoMision.Acertijo);
        mision.PistaClave.Should().Be("nueva-pista");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarMision_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaConMision(out var etapaId, out var misionId);

        Action accion = () => busqueda.ModificarMision(etapaId, misionId, titulo, "Desc", TipoMision.PistaTexto, "pista");

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarMision_PistaClaveVaciaOEspacios_LanzaExcepcionDominio(string pistaClave)
    {
        var busqueda = BusquedaConMision(out var etapaId, out var misionId);

        Action accion = () => busqueda.ModificarMision(etapaId, misionId, "Título", "Desc", TipoMision.PistaTexto, pistaClave);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarMision_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConMision(out _, out var misionId);

        Action accion = () => busqueda.ModificarMision(Guid.NewGuid(), misionId, "Título", "Desc", TipoMision.PistaTexto, "pista");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void ModificarMision_MisionInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConMision(out var etapaId, out _);

        Action accion = () => busqueda.ModificarMision(etapaId, Guid.NewGuid(), "Título", "Desc", TipoMision.PistaTexto, "pista");

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    // --- EliminarMision ---

    [Fact]
    public void EliminarMision_MisionExistente_ReduceConteoMisiones()
    {
        var busqueda = BusquedaConMision(out var etapaId, out var misionId);

        busqueda.EliminarMision(etapaId, misionId);

        busqueda.Etapas.First(e => e.Id == etapaId).Misiones.Should().BeEmpty();
    }

    [Fact]
    public void EliminarMision_MisionInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConMision(out var etapaId, out _);

        Action accion = () => busqueda.EliminarMision(etapaId, Guid.NewGuid());

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void EliminarMision_BusquedaArchivada_LanzaExcepcionDominio()
    {
        var etapaId = Guid.NewGuid();
        var misionId = Guid.NewGuid();
        var mision = Mision.Reconstituir(misionId, etapaId, "Misión", "Desc", TipoMision.PistaTexto, "pista");
        var etapa = Etapa.Reconstituir(etapaId, Guid.NewGuid(), "Etapa", "Desc", 1, new[] { mision });
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Archivada, FechaFija,
            new[] { etapa });

        Action accion = () => busqueda.EliminarMision(etapaId, misionId);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
