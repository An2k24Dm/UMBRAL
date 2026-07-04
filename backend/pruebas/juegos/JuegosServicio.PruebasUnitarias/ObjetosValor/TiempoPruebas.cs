using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.ObjetosValor;

// Pruebas del objeto de valor Tiempo: reglas por contexto e igualdad por valor.
public class TiempoPruebas
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CrearPositivo_MenorOIgualACero_LanzaExcepcionDominio(int valor)
    {
        Action accion = () => Tiempo.CrearPositivo(valor);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    public void CrearPositivo_Positivo_AsignaValor(int valor)
    {
        Tiempo.CrearPositivo(valor).Valor.Should().Be(valor);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(601)]
    public void CrearParaPregunta_FueraDeRango_LanzaExcepcionDominio(int segundos)
    {
        Action accion = () => Tiempo.CrearParaPregunta(segundos);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(600)]
    public void CrearParaPregunta_EnRango_AsignaValor(int segundos)
    {
        Tiempo.CrearParaPregunta(segundos).Valor.Should().Be(segundos);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public void CrearParaBusqueda_MenorAlMinimo_LanzaExcepcionDominio(int minutos)
    {
        Action accion = () => Tiempo.CrearParaBusqueda(minutos);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    public void CrearParaBusqueda_ValorValido_AsignaValor(int minutos)
    {
        Tiempo.CrearParaBusqueda(minutos).Valor.Should().Be(minutos);
    }

    // Rehidratación: acepta datos legacy como 0 (default de BD).
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(700)]
    public void DesdePersistencia_ValorNoNegativo_AsignaValor(int valor)
    {
        Tiempo.DesdePersistencia(valor).Valor.Should().Be(valor);
    }

    [Fact]
    public void DesdePersistencia_Negativo_LanzaExcepcionDominio()
    {
        Action accion = () => Tiempo.DesdePersistencia(-1);
        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Equals_MismoValor_SonIguales()
    {
        var a = Tiempo.CrearParaPregunta(30);
        var b = Tiempo.DesdePersistencia(30);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DistintoValor_NoSonIguales()
    {
        var a = Tiempo.CrearParaPregunta(30);
        var b = Tiempo.CrearParaPregunta(45);

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
    {
        Tiempo.CrearPositivo(45).ToString().Should().Be("45");
    }
}
