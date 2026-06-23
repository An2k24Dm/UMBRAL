using FluentAssertions;
using IdentidadServicio.Aplicacion.Comandos.ModificarOperador;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU09 — pruebas del validador asíncrono de unicidad. Reemplaza al método
// privado ValidarDuplicadosAsync que vivía dentro del manejador.
public class ValidadorUnicidadModificarOperadorPruebas
{
    private readonly Mock<IRepositorioUnicidadUsuario> _unicidad = new();

    private ValidadorUnicidadModificarOperador CrearValidador() =>
        new(_unicidad.Object);

    private static ModificarOperadorComando Comando(Guid id, ModificarOperadorSolicitudDto dto) =>
        new(id, dto);

    [Fact]
    public async Task RechazaNombreUsuarioDuplicadoEnOtroUsuario()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteNombreUsuarioEnOtroUsuarioAsync(
                "ada.admin", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarOperadorSolicitudDto { NombreUsuario = "ada.admin" }),
            CancellationToken.None);

        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoNombreUsuario &&
            e.Mensaje == MensajesValidacionUsuario.NombreUsuarioDuplicado);
    }

    [Fact]
    public async Task RechazaCorreoDuplicadoEnOtroUsuario()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteCorreoEnOtroUsuarioAsync(
                "ada@umbral.com", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarOperadorSolicitudDto { Correo = "ada@umbral.com" }),
            CancellationToken.None);

        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoCorreo &&
            e.Mensaje == MensajesValidacionUsuario.CorreoDuplicado);
    }

    [Fact]
    public async Task RechazaTelefonoDuplicadoEnOtroUsuario()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteTelefonoEnOtroUsuarioAsync(
                "04141234567", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarOperadorSolicitudDto
            {
                DatosContacto = new DatosContactoDto { Telefono = "04141234567" }
            }),
            CancellationToken.None);

        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoTelefono &&
            e.Mensaje == MensajesValidacionUsuario.TelefonoDuplicado);
    }

    [Fact]
    public async Task PermiteValoresIgualesAlPropioOperador()
    {
        // El puerto responde false porque excluye el id actual. El validador
        // no debe reportar errores.
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteCorreoEnOtroUsuarioAsync(
                "operador@umbral.com", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarOperadorSolicitudDto { Correo = "operador@umbral.com" }),
            CancellationToken.None);

        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public async Task NoConsultaCamposNoEnviados()
    {
        var id = Guid.NewGuid();

        await CrearValidador().ValidarAsync(
            Comando(id, new ModificarOperadorSolicitudDto { Nombre = "Olivia" }),
            CancellationToken.None);

        _unicidad.Verify(u => u.ExisteNombreUsuarioEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unicidad.Verify(u => u.ExisteCorreoEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unicidad.Verify(u => u.ExisteTelefonoEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
