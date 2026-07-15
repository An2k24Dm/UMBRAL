using System;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class NombreEquipoPruebas
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_VacioOWhitespace_Lanza(string? valor)
    {
        Action accion = () => NombreEquipo.Crear(valor);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void Crear_AplicaTrim()
    {
        var nombre = NombreEquipo.Crear("  Rojo  ");
        nombre.Valor.Should().Be("Rojo");
    }

    [Fact]
    public void Crear_MasDe80Caracteres_Lanza()
    {
        var largo = new string('a', 81);
        Action accion = () => NombreEquipo.Crear(largo);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void Crear_Exactamente80Caracteres_EsValido()
    {
        var limite = new string('a', 80);
        NombreEquipo.Crear(limite).Valor.Should().HaveLength(80);
    }

    [Fact]
    public void Igualdad_PorValor_CaseInsensitive()
    {
        NombreEquipo.Crear("Rojo").Should().Be(NombreEquipo.Crear("rojo"));
    }
}

public class ContrasenaEquipoHashPruebas
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_VacioOWhitespace_Lanza(string? valor)
    {
        Action accion = () => ContrasenaEquipoHash.Crear(valor);
        accion.Should().Throw<EquipoInvalidoExcepcion>();
    }

    [Fact]
    public void Crear_GuardaElHash()
    {
        var hash = ContrasenaEquipoHash.Crear("$hash$abc123");
        hash.Valor.Should().Be("$hash$abc123");
    }

    [Fact]
    public void ToString_NoExponeElHashNiLaContrasena()
    {
        var hash = ContrasenaEquipoHash.Crear("secreto-hash");
        hash.ToString().Should().NotContain("secreto");
    }

    [Fact]
    public void Igualdad_PorValor()
    {
        ContrasenaEquipoHash.Crear("abc").Should().Be(ContrasenaEquipoHash.Crear("abc"));
    }
}
