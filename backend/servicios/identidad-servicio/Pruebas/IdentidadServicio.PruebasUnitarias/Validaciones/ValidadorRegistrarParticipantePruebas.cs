using FluentAssertions;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU03 — pruebas del validador del registro público de Participante. Reglas
// idénticas a HU02 sobre los campos comunes, más las del alias. El TipoUsuario
// no aplica aquí: HU03 siempre registra Participante.
public class ValidadorRegistrarParticipantePruebas
{
    private readonly Mock<IRepositorioIdentidad> _repositorio = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    private ValidadorRegistrarParticipante CrearValidador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        return new ValidadorRegistrarParticipante(_repositorio.Object, _reloj.Object);
    }

    private static RegistrarParticipanteDto DtoValido() => new()
    {
        Alias = "sombra01",
        NombreUsuario = "participante01",
        Correo = "participante01@umbral.com",
        Contrasena = "Abc1*",
        Nombre = "Pablo",
        Apellido = "Participante",
        Sexo = "Masculino",
        FechaNacimiento = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto
        {
            Direccion = "Av. Bolívar, Caracas",
            Telefono = "04143710260"
        }
    };

    private async Task<List<ErrorValidacion>> ValidarYObtenerErroresAsync(RegistrarParticipanteDto dto)
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

    // ---------- Alias ----------

    [Fact]
    public async Task Falla_Si_AliasVacio()
    {
        var dto = DtoValido(); dto.Alias = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "alias", MensajesValidacionUsuario.AliasObligatorio).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_AliasMuyCorto()
    {
        var dto = DtoValido(); dto.Alias = "ab";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "alias", MensajesValidacionUsuario.AliasLongitud).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_AliasFormatoInvalido()
    {
        var dto = DtoValido(); dto.Alias = "alias con espacios";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "alias", MensajesValidacionUsuario.AliasFormato).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_AliasDuplicado()
    {
        _repositorio.Setup(r => r.ExisteAliasAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoValido());
        TieneError(errores, "alias", MensajesValidacionUsuario.AliasDuplicado).Should().BeTrue();
    }

    // ---------- NombreUsuario ----------

    [Fact]
    public async Task Falla_Si_NombreUsuarioVacio()
    {
        var dto = DtoValido(); dto.NombreUsuario = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_NombreUsuarioFormatoInvalido()
    {
        var dto = DtoValido(); dto.NombreUsuario = "ab";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioFormato)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_NombreUsuarioDuplicado()
    {
        _repositorio.Setup(r => r.ExisteNombreUsuarioAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoValido());
        TieneError(errores, "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioDuplicado)
            .Should().BeTrue();
    }

    // ---------- Correo ----------

    [Fact]
    public async Task Falla_Si_CorreoVacio()
    {
        var dto = DtoValido(); dto.Correo = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "correo", MensajesValidacionUsuario.CorreoObligatorio).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_CorreoFormatoInvalido()
    {
        var dto = DtoValido(); dto.Correo = "no-es-correo";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "correo", MensajesValidacionUsuario.CorreoFormato).Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_CorreoDuplicado()
    {
        _repositorio.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoValido());
        TieneError(errores, "correo", MensajesValidacionUsuario.CorreoDuplicado).Should().BeTrue();
    }

    // ---------- Contraseña ----------

    [Fact]
    public async Task Falla_Si_ContrasenaVacia()
    {
        var dto = DtoValido(); dto.Contrasena = "";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaObligatoria)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ContrasenaSinNumero()
    {
        var dto = DtoValido(); dto.Contrasena = "Abcd*";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaSinNumero)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_ContrasenaSinEspecial()
    {
        var dto = DtoValido(); dto.Contrasena = "Abcd1";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "contrasena", MensajesValidacionUsuario.ContrasenaSinEspecial)
            .Should().BeTrue();
    }

    // ---------- Teléfono ----------

    [Fact]
    public async Task Falla_Si_TelefonoCodigoInvalido()
    {
        var dto = DtoValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "Av. Bolívar", Telefono = "03123710260" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoCodigoInvalido)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_TelefonoDuplicado()
    {
        _repositorio.Setup(r => r.ExisteTelefonoAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var errores = await ValidarYObtenerErroresAsync(DtoValido());
        TieneError(errores, "datosContacto.telefono", MensajesValidacionUsuario.TelefonoDuplicado)
            .Should().BeTrue();
    }

    // ---------- Dirección ----------

    [Fact]
    public async Task Falla_Si_DireccionVacia()
    {
        var dto = DtoValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "", Telefono = "04143710260" };
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "datosContacto.direccion",
            MensajesValidacionUsuario.DireccionObligatoria).Should().BeTrue();
    }

    // ---------- Fecha de nacimiento ----------

    [Fact]
    public async Task Falla_Si_FechaNacimientoFutura()
    {
        var dto = DtoValido();
        dto.FechaNacimiento = Ahora.AddYears(1);
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "fechaNacimiento", MensajesValidacionUsuario.FechaNacimientoFutura)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_MenorDe18()
    {
        var dto = DtoValido();
        dto.FechaNacimiento = Ahora.AddYears(-17);
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "fechaNacimiento", MensajesValidacionUsuario.EdadMinima)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Falla_Si_MayorDe100()
    {
        var dto = DtoValido();
        dto.FechaNacimiento = Ahora.AddYears(-101);
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "fechaNacimiento", MensajesValidacionUsuario.EdadMaxima)
            .Should().BeTrue();
    }

    // ---------- Sexo ----------

    [Fact]
    public async Task Falla_Si_SexoInvalido()
    {
        var dto = DtoValido();
        dto.Sexo = "Marciano";
        var errores = await ValidarYObtenerErroresAsync(dto);
        TieneError(errores, "sexo", MensajesValidacionUsuario.SexoInvalido).Should().BeTrue();
    }

    // ---------- Caso feliz ----------

    [Fact]
    public async Task Pasa_ConDatosValidosDeParticipante()
    {
        var errores = await ValidarYObtenerErroresAsync(DtoValido());
        errores.Should().BeEmpty();
    }

    [Fact]
    public async Task Normaliza_Telefono_QuitandoEspaciosYGuiones()
    {
        var dto = DtoValido();
        dto.DatosContacto = new DatosContactoDto
        {
            Direccion = "Av. Bolívar, Caracas",
            Telefono = "0414-371 0260"
        };
        await CrearValidador().ValidarAsync(dto, CancellationToken.None);
        dto.DatosContacto.Telefono.Should().Be("04143710260");
    }
}
