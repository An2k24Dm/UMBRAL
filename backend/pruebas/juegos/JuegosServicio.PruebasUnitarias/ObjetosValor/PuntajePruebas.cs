using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.ObjetosValor;

// Pruebas del objeto de valor Puntaje: reglas por contexto e igualdad por valor.
public class PuntajePruebas
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(11)]
    public void CrearParaPregunta_NoMultiploDe5ONoPositivo_LanzaExcepcionDominio(int valor)
    {
        Action accion = () => Puntaje.CrearParaPregunta(valor);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void CrearParaPregunta_MayorA100_LanzaExcepcionDominio()
    {
        Action accion = () => Puntaje.CrearParaPregunta(105);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(100)]
    public void CrearParaPregunta_MultiploDe5EnRango_AsignaValor(int valor)
    {
        Puntaje.CrearParaPregunta(valor).Valor.Should().Be(valor);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public void CrearParaBusqueda_MenorAlMinimo_LanzaExcepcionDominio(int valor)
    {
        Action accion = () => Puntaje.CrearParaBusqueda(valor);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    public void CrearParaBusqueda_ValorValido_AsignaValor(int valor)
    {
        Puntaje.CrearParaBusqueda(valor).Valor.Should().Be(valor);
    }

    // Rehidratación: acepta datos legacy como 0 o valores no múltiplos de 5.
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(150)]
    public void DesdePersistencia_ValorNoNegativo_AsignaValor(int valor)
    {
        Puntaje.DesdePersistencia(valor).Valor.Should().Be(valor);
    }

    [Fact]
    public void DesdePersistencia_Negativo_LanzaExcepcionDominio()
    {
        Action accion = () => Puntaje.DesdePersistencia(-1);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Cero_TieneValorCero()
    {
        Puntaje.Cero.Valor.Should().Be(0);
    }

    [Fact]
    public void Equals_MismoValor_SonIguales()
    {
        var a = Puntaje.CrearParaPregunta(10);
        var b = Puntaje.DesdePersistencia(10);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DistintoValor_NoSonIguales()
    {
        var a = Puntaje.CrearParaPregunta(10);
        var b = Puntaje.CrearParaPregunta(15);

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
    {
        Puntaje.CrearParaPregunta(25).ToString().Should().Be("25");
    }
}
