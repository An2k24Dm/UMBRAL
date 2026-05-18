using System.Linq;
using FluentAssertions;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;

namespace IdentidadServicio.PruebasUnitarias.Dominio;

public class UsuarioPruebas
{
    private static DateTime Ahora => new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime Nacimiento => new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Administrador AdminValido() => Administrador.Crear(
        NombreUsuario.Crear("admin_umbral"),
        Correo.Crear("admin@umbral.com"),
        NombrePersona.Crear("Ada", "Admin"),
        DatosContacto.Vacio(),
        SexoPersona.Femenino,
        Nacimiento,
        codigoAdministrador: "ADM-001",
        fechaRegistro: Ahora);

    [Fact]
    public void UsuarioActivo_PuedeIniciarSesion()
    {
        var admin = AdminValido();
        admin.Rol.Should().Be(RolUsuario.Administrador);
        admin.Estado.Should().Be(EstadoUsuario.Activo);
        admin.PuedeIniciarSesion().Should().BeTrue();
    }

    [Fact]
    public void UsuarioInactivo_NoPuedeIniciarSesion()
    {
        var admin = AdminValido();
        admin.Desactivar();
        admin.Invoking(u => u.ValidarPuedeIniciarSesion())
             .Should().Throw<CuentaDesactivadaExcepcion>();
    }

    [Theory]
    [InlineData("operador01")]
    [InlineData("participante123")]
    [InlineData("admin_umbral")]
    [InlineData("ada.admin")]
    public void NombreUsuario_AceptaIdentificadoresAlfanumericos(string valor)
    {
        var nombre = NombreUsuario.Crear(valor);
        nombre.Valor.Should().Be(valor.ToLowerInvariant());
    }

    [Fact]
    public void NombreUsuario_NoObligaAEmail()
    {
        // No requiere "@" ni dominio.
        var nombre = NombreUsuario.Crear("operador01");
        nombre.Valor.Should().NotContain("@");
    }

    [Fact]
    public void NombreUsuario_RechazaVacio()
    {
        Action accion = () => NombreUsuario.Crear("");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void Correo_RechazaFormatoInvalido()
    {
        Action accion = () => Correo.Crear("no-es-email");
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void Correo_EsObjetoDeValorIndependiente()
    {
        typeof(Correo).Namespace.Should().Be("IdentidadServicio.Dominio.ObjetosDeValor");
        typeof(Correo).GetProperty("Valor").Should().NotBeNull();
    }

    [Fact]
    public void DatosContacto_NoTieneCorreoNiEmail()
    {
        typeof(DatosContacto).GetProperty("Correo").Should().BeNull();
        typeof(DatosContacto).GetProperty("Email").Should().BeNull();
    }

    [Fact]
    public void Usuario_NoTieneIdKeycloak()
    {
        typeof(Usuario).GetProperty("IdKeycloak").Should().BeNull(
            "IdKeycloak es un detalle de persistencia, no pertenece al dominio.");
    }

    [Fact]
    public void Herencia_UsuarioEsAbstracta()
    {
        typeof(Usuario).IsAbstract.Should().BeTrue();
        typeof(Administrador).BaseType.Should().Be(typeof(Usuario));
        typeof(Operador).BaseType.Should().Be(typeof(Usuario));
        typeof(Participante).BaseType.Should().Be(typeof(Usuario));
    }

    [Fact]
    public void CrearOperador_SinCodigoOperador_Lanza()
    {
        Action accion = () => Operador.Crear(
            NombreUsuario.Crear("operador01"),
            Correo.Crear("op@umbral.com"),
            NombrePersona.Crear("Olivia", "Op"),
            DatosContacto.Vacio(),
            SexoPersona.Indefinido,
            Nacimiento,
            codigoOperador: "",
            fechaRegistro: Ahora);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void CrearParticipante_SinAlias_Lanza()
    {
        Action accion = () => Participante.Crear(
            NombreUsuario.Crear("participante01"),
            Correo.Crear("par@umbral.com"),
            NombrePersona.Crear("Pablo", "Par"),
            DatosContacto.Vacio(),
            SexoPersona.Indefinido,
            Nacimiento,
            alias: "",
            fechaRegistro: Ahora);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }
}
