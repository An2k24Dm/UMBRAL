using FluentAssertions;
using IdentidadServicio.Aplicacion.Comandos.ModificarOperador;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU09 — pruebas del validador de edición parcial del Operador tras el
// refactor de validaciones. El validador no exige campos no enviados, valida
// con reglas comunes los que sí llegaron y no toca Estado/FechaRegistro/Rol
// (esos campos no existen en el DTO).
public class ValidadorModificarOperadorPruebas
{
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);

    private ValidadorModificarOperador CrearValidador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        return new ValidadorModificarOperador(new ReglasValidacionUsuario(_reloj.Object));
    }

    private static ModificarOperadorComando Comando(ModificarOperadorSolicitudDto dto) =>
        new(Guid.NewGuid(), dto);

    [Fact]
    public void DtoVacio_NoReportaErrores()
    {
        // Edición parcial: si no se envía ningún campo, no hay nada que validar.
        // El "no había cambios" lo resuelve el manejador después.
        var resultado = CrearValidador().Validar(Comando(new ModificarOperadorSolicitudDto()));
        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public void SoloNombre_ValidaSoloNombre()
    {
        var dto = new ModificarOperadorSolicitudDto { Nombre = "Olivia" };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public void NombreVacio_Rechaza()
    {
        var dto = new ModificarOperadorSolicitudDto { Nombre = "" };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e =>
            e.Campo == "nombre" && e.Mensaje == MensajesValidacionUsuario.NombreObligatorio);
    }

    [Fact]
    public void CorreoNulo_NoSeValida()
    {
        var dto = new ModificarOperadorSolicitudDto { Nombre = "Olivia" };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().NotContain(e => e.Campo == "correo");
    }

    [Fact]
    public void CorreoFormatoInvalido_Rechaza()
    {
        var dto = new ModificarOperadorSolicitudDto { Correo = "no-es-correo" };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e =>
            e.Campo == "correo" && e.Mensaje == MensajesValidacionUsuario.CorreoFormato);
    }

    [Fact]
    public void TelefonoSeNormaliza_AntesDeValidar()
    {
        var dto = new ModificarOperadorSolicitudDto
        {
            DatosContacto = new DatosContactoDto { Telefono = "0414-371 0260" }
        };
        var resultado = CrearValidador().Validar(Comando(dto));
        dto.DatosContacto!.Telefono.Should().Be("04143710260");
        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public void DireccionEnviadaCorta_Rechaza()
    {
        var dto = new ModificarOperadorSolicitudDto
        {
            DatosContacto = new DatosContactoDto { Direccion = "ABC" }
        };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e =>
            e.Campo == "datosContacto.direccion" &&
            e.Mensaje == MensajesValidacionUsuario.DireccionLongitud);
    }

    [Fact]
    public void FechaFutura_Rechaza()
    {
        var dto = new ModificarOperadorSolicitudDto { FechaNacimiento = Ahora.AddYears(1) };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e => e.Campo == "fechaNacimiento");
    }

    [Fact]
    public void SexoInvalido_Rechaza()
    {
        var dto = new ModificarOperadorSolicitudDto { Sexo = "Marciano" };
        var resultado = CrearValidador().Validar(Comando(dto));
        resultado.Errores.Should().Contain(e => e.Campo == "sexo");
    }

}
