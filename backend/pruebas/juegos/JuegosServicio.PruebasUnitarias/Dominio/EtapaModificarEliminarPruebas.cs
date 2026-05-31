using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU24/HU29: pruebas de BusquedaTesoro.ModificarEtapa y EliminarEtapa.
public class EtapaModificarEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConEtapa(out Guid etapaId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa original", "Descripción original");
        etapaId = etapa.Id;
        return busqueda;
    }

    // --- ModificarEtapa ---

    [Fact]
    public void ModificarEtapa_ConDatosValidos_ActualizaTituloDescripcionYOrden()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        busqueda.ModificarEtapa(etapaId, "Nuevo título", "Nueva descripción", 1);

        var etapa = busqueda.Etapas.First(e => e.Id == etapaId);
        etapa.Titulo.Should().Be("Nuevo título");
        etapa.Descripcion.Should().Be("Nueva descripción");
        etapa.Orden.Should().Be(1);
    }

    [Fact]
    public void ModificarEtapa_TituloConEspacios_NormalizaConTrim()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        busqueda.ModificarEtapa(etapaId, "  Título con espacios  ", "Descripción", 1);

        var etapa = busqueda.Etapas.First(e => e.Id == etapaId);
        etapa.Titulo.Should().Be("Título con espacios");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarEtapa_TituloVacioOEspacios_LanzaExcepcionDominio(string titulo)
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        Action accion = () => busqueda.ModificarEtapa(etapaId, titulo, "Descripción", 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarEtapa_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapa(out _);

        Action accion = () => busqueda.ModificarEtapa(Guid.NewGuid(), "Título", "Descripción", 1);

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void ModificarEtapa_BusquedaActiva_LanzaExcepcionDominio()
    {
        var etapaId = Guid.NewGuid();
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Activa, FechaFija,
            new[] { Etapa.Reconstituir(etapaId, Guid.NewGuid(), "Etapa", "Desc", 1, []) });

        Action accion = () => busqueda.ModificarEtapa(etapaId, "Nuevo título", "Nueva descripción", 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    // HU29 — validación de orden en modificación
    [Fact]
    public void ModificarEtapa_OrdenColisionaConOtraEtapa_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AgregarEtapa("Etapa A", "Descripción");       // orden 1
        var etapa2 = busqueda.AgregarEtapa("Etapa B", "Descripción"); // orden 2

        // Intentar cambiar etapa2 al orden 1 (ya ocupado por etapa1)
        Action accion = () => busqueda.ModificarEtapa(etapa2.Id, "Etapa B", "Descripción", 1);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarEtapa_MismoOrdenQueActual_NoLanzaExcepcion()
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa A", "Descripción"); // orden 1

        Action accion = () => busqueda.ModificarEtapa(etapa.Id, "Nuevo título", "Nueva desc", 1);

        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ModificarEtapa_OrdenMenorOIgualACero_LanzaExcepcionDominio(int orden)
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        Action accion = () => busqueda.ModificarEtapa(etapaId, "Título", "Descripción", orden);

        accion.Should().Throw<ExcepcionDominio>();
    }

    // --- EliminarEtapa ---

    [Fact]
    public void EliminarEtapa_EtapaExistente_ReduceConteoDeEtapas()
    {
        var busqueda = BusquedaConEtapa(out var etapaId);

        busqueda.EliminarEtapa(etapaId);

        busqueda.Etapas.Should().BeEmpty();
    }

    [Fact]
    public void EliminarEtapa_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapa(out _);

        Action accion = () => busqueda.EliminarEtapa(Guid.NewGuid());

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void EliminarEtapa_BusquedaActiva_LanzaExcepcionDominio()
    {
        var etapaId = Guid.NewGuid();
        var busqueda = BusquedaTesoro.Reconstituir(
            Guid.NewGuid(), "Búsqueda", "Descripción",
            Guid.NewGuid(), EstadoBusqueda.Activa, FechaFija,
            new[] { Etapa.Reconstituir(etapaId, Guid.NewGuid(), "Etapa", "Desc", 1, []) });

        Action accion = () => busqueda.EliminarEtapa(etapaId);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
