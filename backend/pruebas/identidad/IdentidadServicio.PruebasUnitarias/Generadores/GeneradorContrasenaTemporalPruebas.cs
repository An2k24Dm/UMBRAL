using IdentidadServicio.Aplicacion.Generadores;

namespace IdentidadServicio.PruebasUnitarias.Generadores;

public class GeneradorContrasenaTemporalPruebas
{
    [Fact]
    public void Generar_ProduceContrasenaDeLongitudYCategoriasRequeridas()
    {
        var generador = new GeneradorContrasenaTemporal();

        var contrasena = generador.Generar();

        contrasena.Should().HaveLength(14);
        contrasena.ToCharArray().Should().Contain(c => char.IsUpper(c));
        contrasena.ToCharArray().Should().Contain(c => char.IsLower(c));
        contrasena.ToCharArray().Should().Contain(c => char.IsDigit(c));
        contrasena.ToCharArray().Should().Contain(c => "!@#%*_-.?".Contains(c));
        contrasena.ToCharArray().Should().OnlyContain(c =>
            char.IsLetterOrDigit(c) || "!@#%*_-.?".Contains(c));
    }

    [Fact]
    public void Generar_NoUsaCaracteresConfusosOProblematicos()
    {
        var generador = new GeneradorContrasenaTemporal();

        var muestras = Enumerable.Range(0, 25)
            .Select(_ => generador.Generar())
            .ToArray();

        muestras.Should().OnlyContain(c => c.Length == 14);
        muestras.SelectMany(c => c).Should().NotContain(new[] { 'I', 'O', 'l', 'o', '0', '1' });
        muestras.SelectMany(c => c).Should().NotContain(new[] { ' ', '"', '\'', '`', '<', '>', '&', '\\', '/', '|', '$', '^' });
    }
}
