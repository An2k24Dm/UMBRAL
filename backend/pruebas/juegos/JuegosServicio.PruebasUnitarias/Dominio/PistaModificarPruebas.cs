using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

public class PistaModificarPruebas
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
    public void ModificarPista_ConContenidoValido_ActualizaContenido()
    {
        var busqueda = BusquedaConPista(out var pistaId);

        busqueda.ModificarPista(pistaId, "Nuevo contenido de pista.", TipoPista.Texto, null, null);

        busqueda.Pistas.First(p => p.Id == pistaId).Contenido
            .Should().Be("Nuevo contenido de pista.");
    }

    [Fact]
    public void ModificarPista_ConEspacios_NormalizaConTrim()
    {
        var busqueda = BusquedaConPista(out var pistaId);

        busqueda.ModificarPista(pistaId, "  Contenido con espacios  ", TipoPista.Texto, null, null);

        busqueda.Pistas.First(p => p.Id == pistaId).Contenido
            .Should().Be("Contenido con espacios");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ModificarPista_ContenidoVacioOEspacios_LanzaExcepcionDominio(string contenido)
    {
        var busqueda = BusquedaConPista(out var pistaId);

        Action accion = () => busqueda.ModificarPista(pistaId, contenido, TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void ModificarPista_PistaInexistente_LanzaExcepcionNoEncontrado()
    {
        var busqueda = BusquedaConPista(out _);

        Action accion = () => busqueda.ModificarPista(Guid.NewGuid(), "Nuevo contenido.", TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionNoEncontrado>();
    }

    [Fact]
    public void ModificarPista_BusquedaActiva_LanzaExcepcionDominio()
    {
        var busqueda = BusquedaConPista(out var pistaId);
        busqueda.Activar();

        Action accion = () => busqueda.ModificarPista(pistaId, "Nuevo contenido.", TipoPista.Texto, null, null);

        accion.Should().Throw<ExcepcionDominio>();
    }
}
