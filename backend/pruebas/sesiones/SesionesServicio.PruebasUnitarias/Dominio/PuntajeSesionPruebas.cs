using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Pruebas del objeto de valor PuntajeSesion: entero nunca negativo,
// inmutable, con igualdad por valor.
public class PuntajeSesionPruebas
{
    [Fact]
    public void Crear_ConCero_EsValido()
    {
        PuntajeSesion.Crear(0).Valor.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Crear_ConValorPositivo_EsValido(int valor)
    {
        PuntajeSesion.Crear(valor).Valor.Should().Be(valor);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Crear_ConValorNegativo_Lanza(int valor)
    {
        Action accion = () => PuntajeSesion.Crear(valor);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void Cero_TieneValorCero()
    {
        PuntajeSesion.Cero().Valor.Should().Be(0);
    }

    [Fact]
    public void DesdePersistencia_CeroYPositivos_SonValidos()
    {
        PuntajeSesion.DesdePersistencia(0).Valor.Should().Be(0);
        PuntajeSesion.DesdePersistencia(42).Valor.Should().Be(42);
    }

    [Fact]
    public void DesdePersistencia_Negativo_Lanza()
    {
        Action accion = () => PuntajeSesion.DesdePersistencia(-1);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void Sumar_Int_AcumulaSinMutarElOriginal()
    {
        var inicial = PuntajeSesion.Crear(10);

        var resultado = inicial.Sumar(5);

        resultado.Valor.Should().Be(15);
        inicial.Valor.Should().Be(10);
    }

    [Fact]
    public void Sumar_OtroPuntaje_Acumula()
    {
        var resultado = PuntajeSesion.Crear(10).Sumar(PuntajeSesion.Crear(7));
        resultado.Valor.Should().Be(17);
    }

    [Fact]
    public void Sumar_Negativo_Lanza()
    {
        var puntaje = PuntajeSesion.Crear(10);
        Action accion = () => puntaje.Sumar(-1);
        accion.Should().Throw<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public void Equals_MismoValor_SonIguales()
    {
        var a = PuntajeSesion.Crear(10);
        var b = PuntajeSesion.DesdePersistencia(10);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DistintoValor_NoSonIguales()
    {
        var a = PuntajeSesion.Crear(10);
        var b = PuntajeSesion.Crear(11);

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
    {
        PuntajeSesion.Crear(25).ToString().Should().Be("25");
    }
}
