using FluentAssertions;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Generadores;

// El generador depende de los repositorios específicos de Operador y
// Administrador para conocer el último código asignado.
public class GeneradorCodigoUsuarioPruebas
{
    private readonly Mock<IRepositorioOperadores> _repoOperadores = new();
    private readonly Mock<IRepositorioAdministradores> _repoAdministradores = new();

    private GeneradorCodigoUsuario CrearGenerador() =>
        new(_repoOperadores.Object, _repoAdministradores.Object);

    // ---- Operador ----

    [Fact]
    public async Task GenerarCodigoOperador_SinPrevios_DevuelveOP001()
    {
        _repoOperadores.Setup(r => r.ObtenerUltimoCodigoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        (await CrearGenerador().GenerarCodigoOperadorAsync(CancellationToken.None))
            .Should().Be("OP-001");
    }

    [Theory]
    [InlineData("OP-001", "OP-002")]
    [InlineData("OP-009", "OP-010")]
    [InlineData("OP-099", "OP-100")]
    [InlineData("OP-123", "OP-124")]
    public async Task GenerarCodigoOperador_ConUltimo_DevuelveSiguiente(string ultimo, string esperado)
    {
        _repoOperadores.Setup(r => r.ObtenerUltimoCodigoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ultimo);

        (await CrearGenerador().GenerarCodigoOperadorAsync(CancellationToken.None))
            .Should().Be(esperado);
    }

    [Fact]
    public async Task GenerarCodigoOperador_UltimoConFormatoInvalido_VuelveA001()
    {
        _repoOperadores.Setup(r => r.ObtenerUltimoCodigoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("legacy-OP");

        (await CrearGenerador().GenerarCodigoOperadorAsync(CancellationToken.None))
            .Should().Be("OP-001");
    }

    // ---- Administrador ----

    [Fact]
    public async Task GenerarCodigoAdministrador_SinPrevios_DevuelveAD001()
    {
        _repoAdministradores.Setup(r => r.ObtenerUltimoCodigoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        (await CrearGenerador().GenerarCodigoAdministradorAsync(CancellationToken.None))
            .Should().Be("AD-001");
    }

    [Theory]
    [InlineData("AD-001", "AD-002")]
    [InlineData("AD-009", "AD-010")]
    [InlineData("AD-099", "AD-100")]
    public async Task GenerarCodigoAdministrador_ConUltimo_DevuelveSiguiente(string ultimo, string esperado)
    {
        _repoAdministradores.Setup(r => r.ObtenerUltimoCodigoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ultimo);

        (await CrearGenerador().GenerarCodigoAdministradorAsync(CancellationToken.None))
            .Should().Be(esperado);
    }
}
