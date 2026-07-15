using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

public class PistaEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConPista(out Guid pistaId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        var pista = busqueda.AgregarPista(null, TipoPista.CoordenadaGps, -34.6037, -58.3816);
        pistaId = pista.Id;
        return busqueda;
    }

    [Fact]
    public void EliminarPista_PistaExistente_ReduceConteoDePistas()
    {
        var busqueda = BusquedaConPista(out var pistaId);

        busqueda.EliminarPista(pistaId);

        busqueda.Pistas.Should().BeEmpty();
    }

    [Fact]
    public void EliminarPista_VariasPistas_SoloEliminaLaIndicada()
    {
        var busqueda = BusquedaConPista(out var pistaId);
        busqueda.AgregarPista("Segunda pista.", TipoPista.Texto, null, null);

        busqueda.EliminarPista(pistaId);

        busqueda.Pistas.Should().HaveCount(1);
    }

    [Fact]
    public void EliminarPista_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConPista(out _);

        Action accion = () => busqueda.EliminarPista(Guid.NewGuid());

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void EliminarPista_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaConPista(out var pistaId);
        busqueda.Activar();

        Action accion = () => busqueda.EliminarPista(pistaId);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
