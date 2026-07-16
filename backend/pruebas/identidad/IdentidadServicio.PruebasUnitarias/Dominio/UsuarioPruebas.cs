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
        DatosContacto.Crear("Av. Bolívar", "04143710260"),
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
        admin.PuedeIniciarSesion().Should().BeFalse();
        admin.PuedeEliminarCuenta().Should().BeFalse();
    }

    [Fact]
    public void Activar_RestauraEstadoActivoYPermiteEliminarCuenta()
    {
        var admin = AdminValido();
        admin.Desactivar();

        admin.Activar();

        admin.Estado.Should().Be(EstadoUsuario.Activo);
        admin.PuedeIniciarSesion().Should().BeTrue();
        admin.PuedeEliminarCuenta().Should().BeTrue();
    }

    [Fact]
    public void ActualizacionesValidas_ReemplazanDatosDelUsuario()
    {
        var admin = AdminValido();
        var nuevoNombreUsuario = NombreUsuario.Crear("admin.nuevo");
        var nuevoCorreo = Correo.Crear("nuevo@umbral.com");
        var nuevaPersona = NombrePersona.Crear("Ada", "Renovada");
        var nuevoContacto = DatosContacto.Crear("Calle 2", "04241234567");
        var nuevoNacimiento = new DateTime(1992, 2, 2, 0, 0, 0, DateTimeKind.Utc);

        admin.ActualizarNombreUsuario(nuevoNombreUsuario);
        admin.ActualizarCorreo(nuevoCorreo);
        admin.ActualizarNombrePersona(nuevaPersona);
        admin.ActualizarDatosContacto(nuevoContacto);
        admin.ActualizarSexo(SexoPersona.Masculino);
        admin.ActualizarFechaNacimiento(nuevoNacimiento);

        admin.NombreUsuario.Should().BeSameAs(nuevoNombreUsuario);
        admin.Correo.Should().BeSameAs(nuevoCorreo);
        admin.NombrePersona.Should().BeSameAs(nuevaPersona);
        admin.DatosContacto.Should().BeSameAs(nuevoContacto);
        admin.Sexo.Should().Be(SexoPersona.Masculino);
        admin.FechaNacimiento.Should().Be(nuevoNacimiento);
    }

    [Fact]
    public void ConstructorProtegeDatosObligatorios()
    {
        Action sinNombreUsuario = () => new Administrador(
            Guid.NewGuid(),
            null!,
            Correo.Crear("admin@umbral.com"),
            EstadoUsuario.Activo,
            Ahora,
            NombrePersona.Crear("Ada", "Admin"),
            DatosContacto.Crear("Av. Bolívar", "04143710260"),
            SexoPersona.Femenino,
            Nacimiento,
            "ADM-001");
        Action sinCorreo = () => new Administrador(
            Guid.NewGuid(),
            NombreUsuario.Crear("admin_umbral"),
            null!,
            EstadoUsuario.Activo,
            Ahora,
            NombrePersona.Crear("Ada", "Admin"),
            DatosContacto.Crear("Av. Bolívar", "04143710260"),
            SexoPersona.Femenino,
            Nacimiento,
            "ADM-001");
        Action sinPersona = () => new Administrador(
            Guid.NewGuid(),
            NombreUsuario.Crear("admin_umbral"),
            Correo.Crear("admin@umbral.com"),
            EstadoUsuario.Activo,
            Ahora,
            null!,
            DatosContacto.Crear("Av. Bolívar", "04143710260"),
            SexoPersona.Femenino,
            Nacimiento,
            "ADM-001");
        Action sinContacto = () => new Administrador(
            Guid.NewGuid(),
            NombreUsuario.Crear("admin_umbral"),
            Correo.Crear("admin@umbral.com"),
            EstadoUsuario.Activo,
            Ahora,
            NombrePersona.Crear("Ada", "Admin"),
            null!,
            SexoPersona.Femenino,
            Nacimiento,
            "ADM-001");

        sinNombreUsuario.Should().Throw<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*nombre de usuario*");
        sinCorreo.Should().Throw<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*correo*");
        sinPersona.Should().Throw<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*persona*");
        sinContacto.Should().Throw<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*contacto*");
    }

    [Fact]
    public void ConstructorRechazaRolYFechaNacimientoInvalidos()
    {
        Action rolInvalido = () => new UsuarioDePrueba(
            (RolUsuario)999,
            EstadoUsuario.Activo,
            Nacimiento);
        Action fechaDefault = () => new UsuarioDePrueba(
            RolUsuario.Administrador,
            EstadoUsuario.Activo,
            default);
        Action fechaFuturaRespectoAlRegistro = () => new UsuarioDePrueba(
            RolUsuario.Administrador,
            EstadoUsuario.Activo,
            Ahora.AddDays(1));

        rolInvalido.Should().Throw<RolNoValidoExcepcion>();
        fechaDefault.Should().Throw<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*fecha de nacimiento*");
        fechaFuturaRespectoAlRegistro.Should().Throw<DatosUsuarioInvalidosExcepcion>()
            .WithMessage("*fecha de nacimiento*");
    }

    [Fact]
    public void ActualizacionesInvalidas_LanzanExcepcionSinCambiarEstado()
    {
        var admin = AdminValido();

        admin.Invoking(a => a.ActualizarNombreUsuario(null!))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();
        admin.Invoking(a => a.ActualizarCorreo(null!))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();
        admin.Invoking(a => a.ActualizarNombrePersona(null!))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();
        admin.Invoking(a => a.ActualizarDatosContacto(null!))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();
        admin.Invoking(a => a.ActualizarSexo((SexoPersona)999))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();
        admin.Invoking(a => a.ActualizarFechaNacimiento(default))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();
        admin.Invoking(a => a.ActualizarFechaNacimiento(Ahora.AddDays(1)))
            .Should().Throw<DatosUsuarioInvalidosExcepcion>();

        admin.Estado.Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public void ValidarPuedeIniciarSesion_RechazaRolNoDefinidoAunqueEsteActivo()
    {
        var usuario = new UsuarioDePrueba(RolUsuario.Administrador, EstadoUsuario.Activo, Nacimiento);
        usuario.ForzarRol((RolUsuario)999);

        usuario.Invoking(u => u.ValidarPuedeIniciarSesion())
            .Should().Throw<RolNoValidoExcepcion>();
        usuario.PuedeIniciarSesion().Should().BeFalse();
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
            DatosContacto.Crear("Av. Bolívar", "04143710260"),
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
            DatosContacto.Crear("Av. Bolívar", "04143710260"),
            SexoPersona.Indefinido,
            Nacimiento,
            alias: "",
            fechaRegistro: Ahora);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    private sealed class UsuarioDePrueba : Usuario
    {
        public UsuarioDePrueba(
            RolUsuario rol,
            EstadoUsuario estado,
            DateTime fechaNacimiento)
            : base(
                Guid.NewGuid(),
                NombreUsuario.Crear("usuario.prueba"),
                Correo.Crear("usuario@umbral.com"),
                rol,
                estado,
                Ahora,
                NombrePersona.Crear("Usuario", "Prueba"),
                DatosContacto.Crear("Av. Bolívar", "04143710260"),
                SexoPersona.Indefinido,
                fechaNacimiento)
        {
        }

        public void ForzarRol(RolUsuario rol) => Rol = rol;
    }
}
