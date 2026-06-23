using FluentAssertions;
using IdentidadServicio.Aplicacion.Comandos.ModificarParticipante;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Validaciones;

// HU10 — pruebas del validador asíncrono de unicidad para Participante.
// Replica el contrato del de Operador con el matiz de que el id del
// Participante se inyecta en el comando antes de invocar (lo hace el
// manejador tras resolver el agregado vía IdKeycloak).
public class ValidadorUnicidadModificarParticipantePruebas
{
    private readonly Mock<IRepositorioUnicidadUsuario> _unicidad = new();

    private ValidadorUnicidadModificarParticipante CrearValidador() => new(_unicidad.Object);

    private static ModificarParticipanteComando Comando(
        Guid idParticipante, ModificarParticipanteSolicitudDto dto)
    {
        var comando = new ModificarParticipanteComando("kc-participante", dto)
        {
            IdParticipanteActual = idParticipante
        };
        return comando;
    }

    [Fact]
    public async Task RechazaNombreUsuarioDuplicadoEnOtroUsuario()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteNombreUsuarioEnOtroUsuarioAsync(
                "tomado", id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto { NombreUsuario = "tomado" }),
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
                "tomado@umbral.com", id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto { Correo = "tomado@umbral.com" }),
            CancellationToken.None);

        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoCorreo);
    }

    [Fact]
    public async Task RechazaTelefonoDuplicadoEnOtroUsuario()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteTelefonoEnOtroUsuarioAsync(
                "04141234567", id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto
            {
                DatosContacto = new DatosContactoDto { Telefono = "04141234567" }
            }),
            CancellationToken.None);

        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoTelefono);
    }

    [Fact]
    public async Task PermiteValoresIgualesAlPropioParticipante()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteCorreoEnOtroUsuarioAsync(
                "pablo@umbral.com", id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto { Correo = "pablo@umbral.com" }),
            CancellationToken.None);

        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public async Task RechazaAliasDuplicadoEnOtroParticipante()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteAliasEnOtroUsuarioAsync(
                "tomado_99", id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto { Alias = "tomado_99" }),
            CancellationToken.None);

        resultado.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoAlias &&
            e.Mensaje == MensajesValidacionUsuario.AliasDuplicado);
    }

    [Fact]
    public async Task PermiteAliasIgualAlPropioParticipante()
    {
        var id = Guid.NewGuid();
        _unicidad.Setup(u => u.ExisteAliasEnOtroUsuarioAsync(
                "mio_actual", id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resultado = await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto { Alias = "mio_actual" }),
            CancellationToken.None);

        resultado.EsValido.Should().BeTrue();
    }

    [Fact]
    public async Task NoConsultaCamposNoEnviados()
    {
        var id = Guid.NewGuid();
        await CrearValidador().ValidarAsync(
            Comando(id, new ModificarParticipanteSolicitudDto { Nombre = "Pablo" }),
            CancellationToken.None);

        _unicidad.Verify(u => u.ExisteNombreUsuarioEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unicidad.Verify(u => u.ExisteCorreoEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unicidad.Verify(u => u.ExisteTelefonoEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unicidad.Verify(u => u.ExisteAliasEnOtroUsuarioAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
