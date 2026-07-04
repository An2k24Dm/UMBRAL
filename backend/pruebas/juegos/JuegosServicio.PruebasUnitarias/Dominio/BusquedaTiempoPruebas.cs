using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Reglas de tiempo de la búsqueda del tesoro: el mínimo de 5 minutos para
// crear/modificar vive ahora en Tiempo.CrearParaBusqueda. El tope máximo
// (60 minutos) se sigue validando en la capa de aplicación.
public class BusquedaTiempoPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro Crear(int tiempo) =>
        BusquedaTesoro.Crear(
            "Búsqueda", "Descripción", CreadorId, FechaFija,
            Tiempo.CrearParaBusqueda(tiempo), Puntaje.CrearParaBusqueda(50));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(4)]
    public void Crear_TiempoMenorAlMinimo_Lanza(int tiempo)
    {
        Action accion = () => Crear(tiempo);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Crear_TiempoValido_NoLanza()
    {
        Action accion = () => Crear(15);
        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void Modificar_TiempoMenorAlMinimo_Lanza(int tiempo)
    {
        var busqueda = Crear(15);
        Action accion = () => busqueda.Modificar(
            "Búsqueda", "Descripción",
            Tiempo.CrearParaBusqueda(tiempo), Puntaje.CrearParaBusqueda(50));
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Modificar_TiempoValido_NoLanza()
    {
        var busqueda = Crear(15);
        Action accion = () => busqueda.Modificar(
            "Búsqueda", "Descripción",
            Tiempo.CrearParaBusqueda(30), Puntaje.CrearParaBusqueda(50));
        accion.Should().NotThrow();
    }
}
