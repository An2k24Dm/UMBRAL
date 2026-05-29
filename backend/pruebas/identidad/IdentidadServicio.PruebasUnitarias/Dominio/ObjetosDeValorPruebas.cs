using FluentAssertions;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.PruebasUnitarias.Dominio;

// Pruebas de los objetos de valor (records). Confirman que:
//   - Son inmutables.
//   - Conservan el método estático Crear con validaciones.
//   - Comparan por valor (igualdad estructural de record).
public class ObjetosDeValorPruebas
{
    // ----------------- NombreUsuario -----------------

    [Fact]
    public void NombreUsuario_CreaValido()
    {
        var nombre = NombreUsuario.Crear("operador01");
        nombre.Valor.Should().Be("operador01");
    }

    [Fact]
    public void NombreUsuario_FallaSiVacio()
    {
        Action accion = () => NombreUsuario.Crear("");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombreUsuario_FallaSiTieneEspacios()
    {
        Action accion = () => NombreUsuario.Crear("nombre con espacios");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombreUsuario_FallaSiMenorA4Caracteres()
    {
        Action accion = () => NombreUsuario.Crear("abc");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombreUsuario_FallaSiMayorA30Caracteres()
    {
        Action accion = () => NombreUsuario.Crear(new string('a', 31));
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombreUsuario_IgualdadEstructural()
    {
        var a = NombreUsuario.Crear("operador01");
        var b = NombreUsuario.Crear("OPERADOR01"); // normalizado a minúsculas
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void NombreUsuario_EsRecord()
    {
        typeof(NombreUsuario).GetMethod("<Clone>$").Should().NotBeNull(
            "los records exponen un método <Clone>$ generado por el compilador.");
    }

    // ----------------- Correo -----------------

    [Fact]
    public void Correo_CreaValido()
    {
        var c = Correo.Crear("usuario@umbral.com");
        c.Valor.Should().Be("usuario@umbral.com");
    }

    [Fact]
    public void Correo_NormalizaAMinusculas()
    {
        Correo.Crear("USER@UMBRAL.COM").Valor.Should().Be("user@umbral.com");
    }

    [Fact]
    public void Correo_FallaSiVacio()
    {
        Action accion = () => Correo.Crear("");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void Correo_FallaSiFormatoInvalido()
    {
        Action accion = () => Correo.Crear("no-es-correo");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void Correo_IgualdadEstructural()
    {
        var a = Correo.Crear("user@umbral.com");
        var b = Correo.Crear("USER@umbral.com");
        a.Should().Be(b);
    }

    // ----------------- NombrePersona -----------------

    [Fact]
    public void NombrePersona_CreaValido()
    {
        var np = NombrePersona.Crear("Angelo", "Di Martino");
        np.Nombre.Should().Be("Angelo");
        np.Apellido.Should().Be("Di Martino");
        np.ObtenerNombreCompleto().Should().Be("Angelo Di Martino");
    }

    [Fact]
    public void NombrePersona_FallaSiNombreVacio()
    {
        Action accion = () => NombrePersona.Crear("", "Pérez");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombrePersona_FallaSiApellidoVacio()
    {
        Action accion = () => NombrePersona.Crear("Ana", "");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombrePersona_FallaSiNombreTieneNumeros()
    {
        Action accion = () => NombrePersona.Crear("Ana1", "Pérez");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombrePersona_FallaSiApellidoTieneCaracteresEspeciales()
    {
        Action accion = () => NombrePersona.Crear("Ana", "Pé@rez");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void NombrePersona_IgualdadEstructural()
    {
        var a = NombrePersona.Crear("Ana", "Pérez");
        var b = NombrePersona.Crear("Ana", "Pérez");
        a.Should().Be(b);
    }

    // ----------------- DatosContacto -----------------

    private const string TelefonoValido = "04143710260";

    [Fact]
    public void DatosContacto_CreaValido()
    {
        var dc = DatosContacto.Crear("Av. Bolívar, Caracas", TelefonoValido);
        dc.Direccion.Should().Be("Av. Bolívar, Caracas");
        dc.Telefono.Should().Be(TelefonoValido);
    }

    [Fact]
    public void DatosContacto_NormalizaTelefonoConEspaciosYGuiones()
    {
        DatosContacto.Crear("Caracas, Venezuela", "0414-371 0260").Telefono
            .Should().Be("04143710260");
    }

    [Fact]
    public void DatosContacto_FallaSiDireccionVacia()
    {
        Action accion = () => DatosContacto.Crear("", TelefonoValido);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void DatosContacto_FallaSiDireccionMuyCorta()
    {
        Action accion = () => DatosContacto.Crear("ABC", TelefonoValido);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void DatosContacto_FallaSiTelefonoVacio()
    {
        Action accion = () => DatosContacto.Crear("Caracas, Venezuela", "");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void DatosContacto_FallaSiTelefonoTieneLetras()
    {
        Action accion = () => DatosContacto.Crear("Caracas, Venezuela", "0414abcdefg");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void DatosContacto_FallaSiTelefonoNoTiene11Digitos()
    {
        Action accion = () => DatosContacto.Crear("Caracas, Venezuela", "04143710");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void DatosContacto_FallaSiTelefonoCodigoInvalido()
    {
        Action accion = () => DatosContacto.Crear("Caracas, Venezuela", "03123710260");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void DatosContacto_IgualdadEstructural()
    {
        var a = DatosContacto.Crear("Av. Bolívar, Caracas", TelefonoValido);
        var b = DatosContacto.Crear("Av. Bolívar, Caracas", TelefonoValido);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void DatosContacto_NoTieneCorreoNiEmail()
    {
        typeof(DatosContacto).GetProperty("Correo").Should().BeNull();
        typeof(DatosContacto).GetProperty("Email").Should().BeNull();
    }
}
