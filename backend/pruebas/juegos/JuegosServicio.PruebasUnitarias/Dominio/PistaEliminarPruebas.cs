using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU32: pruebas de BusquedaTesoro.EliminarPista.
public class PistaEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConEtapaYPista(out Guid etapaId, out Guid pistaId)
    {
        var busqueda = BusquedaTesoro.Crear(
            "Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var etapa = busqueda.AgregarEtapa("Etapa 1", "Primera etapa");
        etapaId = etapa.Id;
        var pista = busqueda.AgregarPistaAEtapa(etapaId, "Pista de prueba.");
        pistaId = pista.Id;
        return busqueda;
    }

    [Fact]
    public void EliminarPista_PistaExistente_ReduceConteoDePistas()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);

        busqueda.EliminarPista(etapaId, pistaId);

        busqueda.Etapas.First(e => e.Id == etapaId).Pistas.Should().BeEmpty();
    }

    [Fact]
    public void EliminarPista_VariasPistas_SoloEliminaLaIndicada()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);
        busqueda.AgregarPistaAEtapa(etapaId, "Segunda pista.");

        busqueda.EliminarPista(etapaId, pistaId);

        busqueda.Etapas.First(e => e.Id == etapaId).Pistas.Should().HaveCount(1);
    }

    [Fact]
    public void EliminarPista_EtapaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapaYPista(out _, out var pistaId);

        Action accion = () => busqueda.EliminarPista(Guid.NewGuid(), pistaId);

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void EliminarPista_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out _);

        Action accion = () => busqueda.EliminarPista(etapaId, Guid.NewGuid());

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void EliminarPista_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaConEtapaYPista(out var etapaId, out var pistaId);
        busqueda.AgregarMisionAEtapa(etapaId, "Misión", "Desc", TipoMision.PistaTexto, "clave");
        busqueda.Activar();

        Action accion = () => busqueda.EliminarPista(etapaId, pistaId);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
