using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// Invariantes de dominio de la búsqueda del tesoro relacionadas al tiempo:
// debe ser mayor a cero. El rango absoluto (5-60 minutos) se valida en la
// capa de aplicación, no en el dominio.
public class BusquedaTiempoPruebas
{
    private static readonly Guid CreadorId = Guid.NewGuid();
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static BusquedaTesoro Crear(int tiempo) =>
        BusquedaTesoro.Crear("Búsqueda", "Descripción", CreadorId, FechaFija, tiempo, 50);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Crear_TiempoMenorOIgualACero_Lanza(int tiempo)
    {
        Action accion = () => Crear(tiempo);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Crear_TiempoPositivo_NoLanza()
    {
        Action accion = () => Crear(15);
        accion.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Modificar_TiempoMenorOIgualACero_Lanza(int tiempo)
    {
        var busqueda = Crear(15);
        Action accion = () => busqueda.Modificar("Búsqueda", "Descripción", tiempo, 50);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Modificar_TiempoPositivo_NoLanza()
    {
        var busqueda = Crear(15);
        Action accion = () => busqueda.Modificar("Búsqueda", "Descripción", 30, 50);
        accion.Should().NotThrow();
    }
}
