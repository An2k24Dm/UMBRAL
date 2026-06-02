using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU32: pruebas de BusquedaTesoro.EliminarPista.
public class PistaEliminarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro BusquedaConPista(out Guid pistaId)
    {
        var busqueda = BusquedaTesoro.Crear("Búsqueda Test", "Descripción", Guid.NewGuid(), FechaFija);
        busqueda.AsignarMision("Misión", "Desc", TipoMision.PalabraClave, "clave");
        var pista = busqueda.AgregarPistaAMision("Pista de prueba.");
        pistaId = pista.Id;
        return busqueda;
    }

    [Fact]
    public void EliminarPista_PistaExistente_ReduceConteoDePistas()
    {
        var busqueda = BusquedaConPista(out var pistaId);

        busqueda.EliminarPista(pistaId);

        busqueda.Mision!.Pistas.Should().BeEmpty();
    }

    [Fact]
    public void EliminarPista_VariasPistas_SoloEliminaLaIndicada()
    {
        var busqueda = BusquedaConPista(out var pistaId);
        busqueda.AgregarPistaAMision("Segunda pista.");

        busqueda.EliminarPista(pistaId);

        busqueda.Mision!.Pistas.Should().HaveCount(1);
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
