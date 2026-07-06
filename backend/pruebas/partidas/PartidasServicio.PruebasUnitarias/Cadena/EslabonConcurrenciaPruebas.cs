using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Dominio.Abstract;

namespace PartidasServicio.PruebasUnitarias.Cadena;

public class EslabonConcurrenciaPruebas
{
    private readonly Mock<IRepositorioRespuestas> _repositorio = new();
    private static readonly Guid SesionId = Guid.NewGuid();
    private static readonly Guid PreguntaId = Guid.NewGuid();
    private static readonly Guid ParticipanteId = Guid.NewGuid();
    private static readonly Guid EquipoId = Guid.NewGuid();

    private EslabonConcurrencia CrearEslabon() => new(_repositorio.Object);

    [Fact]
    public async Task ValidarAsync_ModoIndividual_PreguntaNoRespondida_MarcaFalse()
    {
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                SesionId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = SesionId,
            PreguntaId = PreguntaId,
            ParticipanteId = ParticipanteId,
            EquipoId = null
        };

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        contexto.PreguntaYaRespondida.Should().BeFalse();
    }

    [Fact]
    public async Task ValidarAsync_ModoIndividual_PreguntaYaRespondida_MarcaTrue()
    {
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                SesionId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = SesionId,
            PreguntaId = PreguntaId,
            ParticipanteId = ParticipanteId,
            EquipoId = null
        };

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        contexto.PreguntaYaRespondida.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_ModoGrupal_PreguntaYaRespondida_MarcaTrue()
    {
        _repositorio.Setup(r => r.YaRespondioEquipoAsync(
                SesionId, PreguntaId, EquipoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = SesionId,
            PreguntaId = PreguntaId,
            ParticipanteId = ParticipanteId,
            EquipoId = EquipoId
        };

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        contexto.PreguntaYaRespondida.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_ModoGrupal_UsaConsultaDeEquipo()
    {
        _repositorio.Setup(r => r.YaRespondioEquipoAsync(
                SesionId, PreguntaId, EquipoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = SesionId,
            PreguntaId = PreguntaId,
            ParticipanteId = ParticipanteId,
            EquipoId = EquipoId
        };

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        _repositorio.Verify(
            r => r.YaRespondioEquipoAsync(SesionId, PreguntaId, EquipoId, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(
            r => r.YaRespondioParticipanteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidarAsync_ModoIndividual_UsaConsultaDeParticipante()
    {
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                SesionId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = SesionId,
            PreguntaId = PreguntaId,
            ParticipanteId = ParticipanteId,
            EquipoId = null
        };

        await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        _repositorio.Verify(
            r => r.YaRespondioParticipanteAsync(SesionId, PreguntaId, ParticipanteId, It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(
            r => r.YaRespondioEquipoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidarAsync_NuncaLanzaExcepcion_AunquePreguntaYaRespondida()
    {
        _repositorio.Setup(r => r.YaRespondioParticipanteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = SesionId,
            PreguntaId = PreguntaId,
            ParticipanteId = ParticipanteId,
            EquipoId = null
        };

        var accion = async () => await CrearEslabon().ValidarAsync(contexto, CancellationToken.None);

        // El eslabón NO lanza: deja que el handler decida
        await accion.Should().NotThrowAsync();
    }
}
