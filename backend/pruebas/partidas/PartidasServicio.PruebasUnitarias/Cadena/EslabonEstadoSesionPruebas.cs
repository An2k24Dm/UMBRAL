using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.PruebasUnitarias.Cadena;

public class EslabonEstadoSesionPruebas
{
    private readonly Mock<IClienteSesiones> _clienteSesiones = new();
    private static readonly Guid SesionId = Guid.NewGuid();
    private static readonly Guid EquipoId = Guid.NewGuid();

    private EslabonEstadoSesion CrearEslabon() => new(_clienteSesiones.Object);

    private static ContextoValidacionRespuesta ContextoBase() => new()
    {
        SesionId = SesionId,
        PreguntaId = Guid.NewGuid(),
        ParticipanteId = Guid.NewGuid()
    };

    [Fact]
    public async Task ValidarAsync_SesionActiva_NoLanzaExcepcion()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto
            {
                Estado = "Activa",
                ParticipanteInscrito = true,
                EquipoId = null
            });

        var accion = async () => await CrearEslabon().ValidarAsync(ContextoBase(), CancellationToken.None);

        await accion.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidarAsync_SesionActiva_PopulaContexto()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto
            {
                Estado = "Activa",
                ParticipanteInscrito = true,
                EquipoId = EquipoId
            });
        var contexto = ContextoBase();

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        contexto.EstadoSesion.Should().Be("Activa");
        contexto.ParticipanteInscrito.Should().BeTrue();
        contexto.EquipoId.Should().Be(EquipoId);
    }

    [Theory]
    [InlineData("Programada")]
    [InlineData("Finalizada")]
    [InlineData("Pausada")]
    [InlineData("EnPreparacion")]
    public async Task ValidarAsync_SesionNoActiva_LanzaSesionNoActivaExcepcion(string estado)
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto
            {
                Estado = estado,
                ParticipanteInscrito = true,
                EquipoId = null
            });

        var accion = async () => await CrearEslabon().ValidarAsync(ContextoBase(), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoActivaExcepcion>();
    }

    [Fact]
    public async Task ValidarAsync_SesionNoEncontrada_LanzaSesionNoActivaExcepcion()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InfoPartidaSesionDto?)null);

        var accion = async () => await CrearEslabon().ValidarAsync(ContextoBase(), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoActivaExcepcion>();
    }

    [Fact]
    public async Task ValidarAsync_LlamaClienteUnaVez()
    {
        _clienteSesiones.Setup(c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InfoPartidaSesionDto { Estado = "Activa", ParticipanteInscrito = true });

        await CrearEslabon().ValidarAsync(ContextoBase(), CancellationToken.None);

        _clienteSesiones.Verify(
            c => c.ObtenerInfoPartidaAsync(SesionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
