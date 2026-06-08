using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class ModificarMisionManejadorPruebas
{
    private readonly Mock<IRepositorioMisiones> _repositorio = new();
    private readonly Mock<IClienteSesiones> _clienteSesiones = new();
    private readonly Mock<IValidador<ModificarMisionComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarMisionManejador CrearManejador() =>
        new(_repositorio.Object, _clienteSesiones.Object, _validador.Object);

    private static Mision MisionInactiva() =>
        Mision.Crear("Misión Original", "Descripción original", Guid.NewGuid(), FechaFija);

    private static ModificarMisionDto DtoValido(NivelDificultad dificultad = NivelDificultad.Baja) =>
        new() { Nombre = "Misión Modificada", Descripcion = "Nueva descripción", Dificultad = (int)dificultad };

    public ModificarMisionManejadorPruebas()
    {
        _clienteSesiones.Setup(c => c.ExisteSesionVigentePorMisionAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _validador.Setup(v => v.Validar(It.IsAny<ModificarMisionComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
        _repositorio
            .Setup(r => r.ActualizarMisionAsync(It.IsAny<Mision>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_MisionInactiva_LlamaActualizarAsyncUnaVez()
    {
        var mision = MisionInactiva();
        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(mision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mision);

        await CrearManejador().Handle(
            new ModificarMisionComando(mision.Id, DtoValido()), CancellationToken.None);

        _repositorio.Verify(
            r => r.ActualizarMisionAsync(mision, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MisionInactiva_ActualizaDatosDominio()
    {
        var mision = MisionInactiva();
        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(mision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mision);

        await CrearManejador().Handle(
            new ModificarMisionComando(mision.Id, DtoValido(NivelDificultad.Dificil)), CancellationToken.None);

        mision.Nombre.Should().Be("Misión Modificada");
        mision.Descripcion.Should().Be("Nueva descripción");
        mision.Dificultad.Should().Be(NivelDificultad.Dificil);
    }

    [Fact]
    public async Task Handle_MisionInexistente_LanzaExcepcionNoEncontrado()
    {
        var misionId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(misionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mision?)null);

        var accion = async () => await CrearManejador()
            .Handle(new ModificarMisionComando(misionId, DtoValido()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
        _repositorio.Verify(
            r => r.ActualizarMisionAsync(It.IsAny<Mision>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MisionActiva_LanzaExcepcionDominio()
    {
        var mision = MisionInactiva();
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        mision.Activar();
        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(mision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mision);

        var accion = async () => await CrearManejador()
            .Handle(new ModificarMisionComando(mision.Id, DtoValido()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
        _repositorio.Verify(
            r => r.ActualizarMisionAsync(It.IsAny<Mision>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
