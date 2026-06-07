using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU02 — pruebas del validador de creación de Operador/Administrador después
// del refactor. El validador ahora:
//  * opera sobre CrearUsuarioComando,
//  * es sincrónico (devuelve ResultadoValidacion en lugar de lanzar),
//  * delega las reglas comunes en IReglasValidacionUsuario (instanciamos la
//    implementación real para garantizar que las reglas comunes funcionan).
//
// Las pruebas de duplicados (correo / nombreUsuario / teléfono) NO viven más
// aquí porque dependen del repositorio: están en CrearUsuarioManejadorPruebas.
public class ValidadorCrearUsuarioPruebas
{
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    private ValidadorCrearUsuario CrearValidador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        return new ValidadorCrearUsuario(new ReglasValidacionUsuario(_reloj.Object));
    }

    private static CrearUsuarioDto DtoOperadorValido() => new()
    {
        TipoUsuario = RolUsuario.Operador,
        NombreUsuario = "operador02",
        Correo = "operador02@gmail.com",
        Nombre = "Angelo",
        Apellido = "Di Martino",
        Sexo = "Masculino",
        FechaNacimiento = new DateTime(2000, 10, 24, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto
        {
            Direccion = "El Paraiso, Caracas, Venezuela",
            Telefono = "04143710260"
        }
    };

    private static CrearUsuarioDto DtoAdministradorValido()
    {
        var dto = DtoOperadorValido();
        dto.TipoUsuario = RolUsuario.Administrador;
        dto.NombreUsuario = "admin02";
        dto.Correo = "admin02@gmail.com";
        dto.Nombre = "Ana";
        dto.Apellido = "Perez";
        dto.Sexo = "Femenino";
        dto.FechaNacimiento = new DateTime(1995, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        dto.DatosContacto = new DatosContactoDto { Direccion = "Caracas", Telefono = "04121234567" };
        return dto;
    }

    private List<ErrorValidacion> Validar(CrearUsuarioDto dto)
        => CrearValidador().Validar(new CrearUsuarioComando(dto)).Errores;

    private static bool TieneError(List<ErrorValidacion> errores, string campo, string mensaje) =>
        errores.Any(e => e.Campo == campo && e.Mensaje == mensaje);

    // ---------- NombreUsuario ----------

    [Fact]
    public void Falla_Si_NombreUsuarioVacio()
    {
        var dto = DtoOperadorValido(); dto.NombreUsuario = "";
        TieneError(Validar(dto), "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_NombreUsuarioFormatoInvalido()
    {
        var dto = DtoOperadorValido(); dto.NombreUsuario = "ab";
        TieneError(Validar(dto), "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioFormato)
            .Should().BeTrue();
    }

    // ---------- Correo ----------

    [Fact]
    public void Falla_Si_CorreoVacio()
    {
        var dto = DtoOperadorValido(); dto.Correo = "";
        TieneError(Validar(dto), "correo", MensajesValidacionUsuario.CorreoObligatorio).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_CorreoFormatoInvalido()
    {
        var dto = DtoOperadorValido(); dto.Correo = "no-es-correo";
        TieneError(Validar(dto), "correo", MensajesValidacionUsuario.CorreoFormato).Should().BeTrue();
    }

    // ---------- Contraseña: el endpoint de creación ya NO la recibe. La
    // contraseña se genera en el backend y se envía por correo. Por eso
    // ya no hay validaciones de contraseña aquí. ----------

    // ---------- Nombre / Apellido ----------

    [Fact]
    public void Falla_Si_NombreVacio()
    {
        var dto = DtoOperadorValido(); dto.Nombre = "";
        TieneError(Validar(dto), "nombre", MensajesValidacionUsuario.NombreObligatorio).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_NombreConNumeros()
    {
        var dto = DtoOperadorValido(); dto.Nombre = "Ana123";
        TieneError(Validar(dto), "nombre", MensajesValidacionUsuario.NombreSoloLetras).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_NombreConCaracteresEspeciales()
    {
        var dto = DtoOperadorValido(); dto.Nombre = "Ana@";
        TieneError(Validar(dto), "nombre", MensajesValidacionUsuario.NombreSoloLetras).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_ApellidoVacio()
    {
        var dto = DtoOperadorValido(); dto.Apellido = "";
        TieneError(Validar(dto), "apellido", MensajesValidacionUsuario.ApellidoObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_ApellidoConNumeros()
    {
        var dto = DtoOperadorValido(); dto.Apellido = "Perez1";
        TieneError(Validar(dto), "apellido", MensajesValidacionUsuario.ApellidoSoloLetras)
            .Should().BeTrue();
    }

    // ---------- Teléfono / Dirección ----------

    [Fact]
    public void Falla_Si_TelefonoVacio()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "" };
        TieneError(Validar(dto), "datosContacto.telefono", MensajesValidacionUsuario.TelefonoObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_TelefonoConLetras()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "0414abcdefg" };
        TieneError(Validar(dto), "datosContacto.telefono", MensajesValidacionUsuario.TelefonoSoloNumeros)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_TelefonoCodigoInvalido()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "03123710260" };
        TieneError(Validar(dto), "datosContacto.telefono", MensajesValidacionUsuario.TelefonoCodigoInvalido)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_TelefonoLongitudInvalida()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "04143710" };
        TieneError(Validar(dto), "datosContacto.telefono", MensajesValidacionUsuario.TelefonoLongitud)
            .Should().BeTrue();
    }

    [Fact]
    public void Normaliza_Telefono_QuitandoEspaciosYGuiones()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto
        {
            Direccion = "Av. Bolívar, Caracas",
            Telefono = "0414-371 0260"
        };
        CrearValidador().Validar(new CrearUsuarioComando(dto));
        dto.DatosContacto.Telefono.Should().Be("04143710260");
    }

    [Fact]
    public void Falla_Si_DireccionVacia()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "", Telefono = "04143710260" };
        TieneError(Validar(dto), "datosContacto.direccion",
            MensajesValidacionUsuario.DireccionObligatoria).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_DireccionMuyCorta()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "ABC", Telefono = "04143710260" };
        TieneError(Validar(dto), "datosContacto.direccion",
            MensajesValidacionUsuario.DireccionLongitud).Should().BeTrue();
    }

    [Fact]
    public void Pasa_DireccionMinima5Caracteres()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "Av. Bolívar", Telefono = "04143710260" };
        Validar(dto).Any(e => e.Campo == "datosContacto.direccion").Should().BeFalse();
    }

    // ---------- Fecha de nacimiento ----------

    [Fact]
    public void Falla_Si_FechaNacimientoFutura()
    {
        var dto = DtoOperadorValido();
        dto.FechaNacimiento = Ahora.AddYears(1);
        TieneError(Validar(dto), "fechaNacimiento", MensajesValidacionUsuario.FechaNacimientoFutura)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_MenorDe18()
    {
        var dto = DtoOperadorValido();
        dto.FechaNacimiento = Ahora.AddYears(-17);
        TieneError(Validar(dto), "fechaNacimiento", MensajesValidacionUsuario.EdadMinima)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_MayorDe100()
    {
        var dto = DtoOperadorValido();
        dto.FechaNacimiento = Ahora.AddYears(-101);
        TieneError(Validar(dto), "fechaNacimiento", MensajesValidacionUsuario.EdadMaxima)
            .Should().BeTrue();
    }

    // ---------- TipoUsuario web ----------

    [Fact]
    public void Falla_Si_TipoUsuarioParticipante_EnRegistroWeb()
    {
        var dto = DtoOperadorValido();
        dto.TipoUsuario = RolUsuario.Participante;
        TieneError(Validar(dto), "tipoUsuario", MensajesValidacionUsuario.TipoUsuarioInvalidoWeb)
            .Should().BeTrue();
    }

    // ---------- Casos felices ----------

    [Fact]
    public void Pasa_ConDatosValidosDeOperador()
    {
        Validar(DtoOperadorValido()).Should().BeEmpty();
    }

    [Fact]
    public void Pasa_ConDatosValidosDeAdministrador()
    {
        Validar(DtoAdministradorValido()).Should().BeEmpty();
    }
}
