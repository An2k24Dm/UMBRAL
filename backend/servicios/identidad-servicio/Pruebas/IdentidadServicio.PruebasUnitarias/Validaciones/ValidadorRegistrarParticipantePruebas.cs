using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU03 — pruebas del validador del registro público de Participante después
// del refactor. Comparte las reglas comunes con HU02 vía
// IReglasValidacionUsuario; aquí solo aseguramos que las reglas particulares
// del Participante (alias) siguen vigentes y que el validador delega
// correctamente las comunes. Las pruebas de duplicados se trasladaron a
// RegistrarParticipanteManejadorPruebas.
public class ValidadorRegistrarParticipantePruebas
{
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    private ValidadorRegistrarParticipante CrearValidador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        return new ValidadorRegistrarParticipante(new ReglasValidacionUsuario(_reloj.Object));
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

    private List<ErrorValidacion> Validar(RegistrarParticipanteDto dto)
        => CrearValidador().Validar(new RegistrarParticipanteComando(dto)).Errores;

    private static bool TieneError(List<ErrorValidacion> errores, string campo, string mensaje) =>
        errores.Any(e => e.Campo == campo && e.Mensaje == mensaje);

    // ---------- Alias (regla particular) ----------

    [Fact]
    public void Falla_Si_AliasVacio()
    {
        var dto = DtoValido(); dto.Alias = "";
        TieneError(Validar(dto), "alias", MensajesValidacionUsuario.AliasObligatorio).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_AliasMuyCorto()
    {
        var dto = DtoValido(); dto.Alias = "ab";
        TieneError(Validar(dto), "alias", MensajesValidacionUsuario.AliasLongitud).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_AliasFormatoInvalido()
    {
        var dto = DtoValido(); dto.Alias = "alias con espacios";
        TieneError(Validar(dto), "alias", MensajesValidacionUsuario.AliasFormato).Should().BeTrue();
    }

    // ---------- Reglas comunes delegadas ----------

    [Fact]
    public void Falla_Si_NombreUsuarioVacio()
    {
        var dto = DtoValido(); dto.NombreUsuario = "";
        TieneError(Validar(dto), "nombreUsuario", MensajesValidacionUsuario.NombreUsuarioObligatorio)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_CorreoFormatoInvalido()
    {
        var dto = DtoValido(); dto.Correo = "no-es-correo";
        TieneError(Validar(dto), "correo", MensajesValidacionUsuario.CorreoFormato).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_ContrasenaSinNumero()
    {
        var dto = DtoValido(); dto.Contrasena = "Abcd*";
        TieneError(Validar(dto), "contrasena", MensajesValidacionUsuario.ContrasenaSinNumero)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_TelefonoCodigoInvalido()
    {
        var dto = DtoValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "Av. Bolívar", Telefono = "03123710260" };
        TieneError(Validar(dto), "datosContacto.telefono", MensajesValidacionUsuario.TelefonoCodigoInvalido)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_DireccionVacia()
    {
        var dto = DtoValido();
        dto.DatosContacto = new DatosContactoDto { Direccion = "", Telefono = "04143710260" };
        TieneError(Validar(dto), "datosContacto.direccion",
            MensajesValidacionUsuario.DireccionObligatoria).Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_FechaNacimientoFutura()
    {
        var dto = DtoValido();
        dto.FechaNacimiento = Ahora.AddYears(1);
        TieneError(Validar(dto), "fechaNacimiento", MensajesValidacionUsuario.FechaNacimientoFutura)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_MenorDe18()
    {
        var dto = DtoValido();
        dto.FechaNacimiento = Ahora.AddYears(-17);
        TieneError(Validar(dto), "fechaNacimiento", MensajesValidacionUsuario.EdadMinima)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_MayorDe100()
    {
        var dto = DtoValido();
        dto.FechaNacimiento = Ahora.AddYears(-101);
        TieneError(Validar(dto), "fechaNacimiento", MensajesValidacionUsuario.EdadMaxima)
            .Should().BeTrue();
    }

    [Fact]
    public void Falla_Si_SexoInvalido()
    {
        var dto = DtoValido();
        dto.Sexo = "Marciano";
        TieneError(Validar(dto), "sexo", MensajesValidacionUsuario.SexoInvalido).Should().BeTrue();
    }

    // ---------- Caso feliz ----------

    [Fact]
    public void Pasa_ConDatosValidosDeParticipante()
    {
        Validar(DtoValido()).Should().BeEmpty();
    }

    [Fact]
    public void Normaliza_Telefono_QuitandoEspaciosYGuiones()
    {
        var dto = DtoValido();
        dto.DatosContacto = new DatosContactoDto
        {
            Direccion = "Av. Bolívar, Caracas",
            Telefono = "0414-371 0260"
        };
        CrearValidador().Validar(new RegistrarParticipanteComando(dto));
        dto.DatosContacto.Telefono.Should().Be("04143710260");
    }
}
