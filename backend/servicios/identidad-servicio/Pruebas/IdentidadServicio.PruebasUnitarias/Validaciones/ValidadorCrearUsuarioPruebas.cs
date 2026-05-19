using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

public class ValidadorCrearUsuarioPruebas
{
    private readonly Mock<IRepositorioIdentidad> _repositorio = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    private ValidadorCrearUsuario CrearValidador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        return new ValidadorCrearUsuario(_repositorio.Object, _reloj.Object);
    }

    private static CrearUsuarioDto DtoOperadorValido() => new()
    {
        TipoUsuario = TipoUsuario.Operador,
        NombreUsuario = "operador02",
        Correo = "operador02@gmail.com",
        Contrasena = "Abc1*",
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
        dto.TipoUsuario = TipoUsuario.Administrador;
        dto.NombreUsuario = "admin02";
        dto.Correo = "admin02@gmail.com";
        dto.Nombre = "Ana";
        dto.Apellido = "Perez";
        dto.Sexo = "Femenino";
        dto.FechaNacimiento = new DateTime(1995, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        dto.DatosContacto = new DatosContactoDto { Direccion = "Caracas", Telefono = "04121234567" };
        return dto;
    }

    private async Task<List<ErrorValidacion>> ValidarYObtenerErroresAsync(CrearUsuarioDto dto)
    {
        try
        {
            await CrearValidador().ValidarAsync(dto, CancellationToken.None);
            return new List<ErrorValidacion>();
        }
        catch (ExcepcionValidacion ex)
        {
            return ex.Errores.ToList();
        }
    }

    private static bool TieneError(List<ErrorValidacion> errores, string campo, string mensaje) =>
        errores.Any(e => e.Campo == campo && e.Mensaje == mensaje);

    // ---------- NombreUsuario ----------

    [Fact]
    public async Task Falla_Si_NombreUsuarioVacio()
    {
        var dto = DtoOperadorValido(); dto.NombreUsuario = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_NombreUsuarioDuplicado()
    {
        _repositorio.Setup(r => r.ExisteNombreUsuarioAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoOperadorValido());
        TieneError(errores, "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioDuplicado)
            .Should().BeTrue();
    }

    // ---------- Correo ----------

    [Fact]
    public async Task Falla_Si_CorreoVacio()
    {
        var dto = DtoOperadorValido(); dto.Correo = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "correo", MensajesValidacionUsuario.CorreoObligatorio).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_CorreoFormatoInvalido()
    {
        var dto = DtoOperadorValido(); dto.Correo = "no-es-correo";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "correo", MensajesValidacionUsuario.CorreoFormato).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_CorreoDuplicado()
    {
        _repositorio.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoOperadorValido());
        TieneError(errores, "correo", MensajesValidacionUsuario.CorreoDuplicado).Should().BeTrue();
    }

    // ---------- Contraseña ----------

    [Fact]
    public async Task Falla_Si_ContrasenaVacia()
    {
        var dto = DtoOperadorValido(); dto.Contrasena = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaObligatoria)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ContrasenaCorta()
    {
        var dto = DtoOperadorValido(); dto.Contrasena = "A1*";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaLongitud)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ContrasenaLarga()
    {
        var dto = DtoOperadorValido(); dto.Contrasena = "Abcdef1234*";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaLongitud)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ContrasenaSinNumero()
    {
        var dto = DtoOperadorValido(); dto.Contrasena = "Abcd*";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaSinNumero)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ContrasenaSinEspecial()
    {
        var dto = DtoOperadorValido(); dto.Contrasena = "Abcd1";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaSinEspecial)
            .Should().BeTrue();
    }

    // ---------- Nombre ----------

    [Fact]
    public async Task Falla_Si_NombreVacio()
    {
        var dto = DtoOperadorValido(); dto.Nombre = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "nombre", MensajesValidacionUsuario.NombreObligatorio).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_NombreConNumeros()
    {
        var dto = DtoOperadorValido(); dto.Nombre = "Ana123";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "nombre", MensajesValidacionUsuario.NombreSoloLetras).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_NombreConCaracteresEspeciales()
    {
        var dto = DtoOperadorValido(); dto.Nombre = "Ana@";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "nombre", MensajesValidacionUsuario.NombreSoloLetras).Should().BeTrue();
    }

    // ---------- Apellido ----------

    [Fact]
    public async Task Falla_Si_ApellidoVacio()
    {
        var dto = DtoOperadorValido(); dto.Apellido = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "apellido", MensajesValidacionUsuario.ApellidoObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ApellidoConNumeros()
    {
        var dto = DtoOperadorValido(); dto.Apellido = "Perez1";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "apellido", MensajesValidacionUsuario.ApellidoSoloLetras)
            .Should().BeTrue();
    }

    // ---------- Teléfono ----------

    [Fact]
    public async Task Falla_Si_TelefonoVacio()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_TelefonoConLetras()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "0414abcdefg" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoSoloNumeros)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_TelefonoCodigoInvalido()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "03123710260" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoCodigoInvalido)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_TelefonoLongitudInvalida()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Telefono = "04143710" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoLongitud)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_TelefonoDuplicado()
    {
        _repositorio.Setup(r => r.ExisteTelefonoAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoOperadorValido());
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoDuplicado)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Normaliza_Telefono_QuitandoEspaciosYGuiones()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto
        {
            Direccion = "Av. Bolívar, Caracas",
            Telefono = "0414-371 0260"
        };
        await CrearValidador().ValidarAsync(dto, CancellationToken.None);
        dto.DatosContacto.Telefono.Should().Be("04143710260");
    }

    // ---------- Fecha de nacimiento ----------

    [Fact]
    public async Task Falla_Si_FechaNacimientoFutura()
    {
        var dto = DtoOperadorValido();
        dto.FechaNacimiento = Ahora.AddYears(1);
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "fechaNacimiento", MensajesValidacionUsuario.FechaNacimientoFutura)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_MenorDe18()
    {
        var dto = DtoOperadorValido();
        dto.FechaNacimiento = Ahora.AddYears(-17);
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "fechaNacimiento", MensajesValidacionUsuario.EdadMinima)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_MayorDe100()
    {
        var dto = DtoOperadorValido();
        dto.FechaNacimiento = Ahora.AddYears(-101);
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "fechaNacimiento", MensajesValidacionUsuario.EdadMaxima)
            .Should().BeTrue();
    }

    // ---------- Dirección ----------

    [Fact]
    public async Task Falla_Si_DireccionVacia()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "", Telefono = "04143710260" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.direccion",
            MensajesValidacionUsuario.DireccionObligatoria).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_DireccionMuyCorta()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "ABC", Telefono = "04143710260" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.direccion",
            MensajesValidacionUsuario.DireccionLongitud).Should().BeTrue();
    }

    [Fact]
    public async Task Pasa_DireccionMinima5Caracteres()
    {
        var dto = DtoOperadorValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "Av. Bolívar", Telefono = "04143710260" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        errores.Any(e => e.Campo == "datosContacto.direccion").Should().BeFalse();
    }

    // ---------- TipoUsuario web ----------

    [Fact]
    public async Task Falla_Si_TipoUsuarioParticipante_EnRegistroWeb()
    {
        var dto = DtoOperadorValido();
        dto.TipoUsuario = TipoUsuario.Participante;
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "tipoUsuario", MensajesValidacionUsuario.TipoUsuarioInvalidoWeb)
            .Should().BeTrue();
    }

    // ---------- Casos felices ----------

    [Fact]
    public async Task Pasa_ConDatosValidosDeOperador()
    {
        var errores = await ValidarYObtenerErroresAsync(DtoOperadorValido());
        errores.Should().BeEmpty();
    }

    [Fact]
    public async Task Pasa_ConDatosValidosDeAdministrador()
    {
        var errores = await ValidarYObtenerErroresAsync(DtoAdministradorValido());
        errores.Should().BeEmpty();
    }
}
