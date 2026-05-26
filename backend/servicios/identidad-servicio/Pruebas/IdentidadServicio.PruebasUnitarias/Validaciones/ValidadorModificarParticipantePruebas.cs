using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU10 — pruebas del validador de formato de edición parcial del
// Participante. Comparte el helper común con HU09, así que los casos
// principales replican los de Operador y aseguran que no se duplicó la
// lógica por accidente.
public class ValidadorModificarParticipantePruebas
{
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);

    private ValidadorModificarParticipante CrearValidador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        return new ValidadorModificarParticipante(new ReglasValidacionUsuario(_reloj.Object));
    }

    private static ModificarParticipanteComando Comando(ModificarParticipanteSolicitudDto dto) =>
        new("kc-participante", dto);

    [Fact]
    public void DtoVacio_NoReportaErrores()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto()));
        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public void Contrasena_AmbosCamposNulos_NoSeValida()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Nombre = "Pablo"
        }));
        resultado.Errores.Should().NotContain(e =>
            e.Campo == MensajesValidacionUsuario.CampoContrasena ||
            e.Campo == MensajesValidacionUsuario.CampoConfirmacionContrasena);
    }

    [Fact]
    public void Contrasena_NoCumpleReglas_Rechaza()
    {
        var dto = new ModificarParticipanteSolicitudDto
        {
            NuevaContrasena = "abc",
            ConfirmacionContrasena = "abc"
        };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoContrasena);
    }

    [Fact]
    public void Contrasena_NoCoincide_Rechaza()
    {
        var dto = new ModificarParticipanteSolicitudDto
        {
            NuevaContrasena = "Abc1*",
            ConfirmacionContrasena = "Otro2*"
        };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoConfirmacionContrasena &&
            e.Mensaje == MensajesValidacionUsuario.ContrasenasNoCoinciden);
    }

    [Fact]
    public void Contrasena_ValidaYCoincide_NoReportaError()
    {
        var dto = new ModificarParticipanteSolicitudDto
        {
            NuevaContrasena = "Abc1*",
            ConfirmacionContrasena = "Abc1*"
        };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public void NoValidaDuplicados()
    {
        // El validador de formato no debe tocar nada parecido a duplicados.
        // Si llega un correo válido, no debe reportar duplicado: eso es
        // responsabilidad del validador de unicidad.
        var dto = new ModificarParticipanteSolicitudDto { Correo = "pablo@umbral.com" };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().NotContain(e =>
            e.Mensaje == MensajesValidacionUsuario.CorreoDuplicado);
    }

    [Fact]
    public void CorreoInvalido_Rechaza()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Correo = "no-es-correo"
        }));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoCorreo &&
            e.Mensaje == MensajesValidacionUsuario.CorreoFormato);
    }

    // ============================================================
    // Reglas de alias en edición (HU10).
    // ============================================================

    [Fact]
    public void Alias_Null_NoSeValida()
    {
        // Si Alias viene null, el validador no reporta nada relacionado.
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Nombre = "Pablo Nuevo"
        }));
        resultado.Errores.Should().NotContain(e => e.Campo == MensajesValidacionUsuario.CampoAlias);
    }

    [Fact]
    public void Alias_Vacio_Rechaza()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Alias = ""
        }));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoAlias &&
            e.Mensaje == MensajesValidacionUsuario.AliasObligatorio);
    }

    [Fact]
    public void Alias_Corto_Rechaza()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Alias = "abc12"
        }));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoAlias &&
            e.Mensaje == MensajesValidacionUsuario.AliasLongitud);
    }

    [Fact]
    public void Alias_Largo_Rechaza()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Alias = "abcdefghijklmnop" // 16
        }));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoAlias &&
            e.Mensaje == MensajesValidacionUsuario.AliasLongitud);
    }

    [Fact]
    public void Alias_CaracteresNoPermitidos_Rechaza()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Alias = "sombra.01"
        }));
        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoAlias &&
            e.Mensaje == MensajesValidacionUsuario.AliasFormato);
    }

    [Fact]
    public void Alias_Valido_Pasa()
    {
        var resultado = CrearValidador().Validar(Comando(new ModificarParticipanteSolicitudDto
        {
            Alias = "sombra_99"
        }));
        resultado.Errores.Should().NotContain(e => e.Campo == MensajesValidacionUsuario.CampoAlias);
    }
}
