using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class EliminarMisionManejadorPruebas
{
    private readonly Mock<IRepositorioMisiones> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private EliminarMisionManejador CrearManejador() => new(_repositorio.Object);

    private static Mision MisionInactiva() =>
        Mision.Crear("Misión Test", "Descripción", Guid.NewGuid(), FechaFija);

    public EliminarMisionManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarMisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_MisionInactiva_LlamaEliminarAsyncUnaVez()
    {
        var mision = MisionInactiva();
        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(mision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mision);

        await CrearManejador().Handle(new EliminarMisionComando(mision.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.EliminarMisionAsync(mision.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MisionInexistente_LanzaExcepcionNoEncontrado()
    {
        var misionId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(misionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mision?)null);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarMisionComando(misionId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_MisionActiva_LanzaExcepcionDominio()
    {
        var mision = MisionInactiva();
        // Agregar etapa para poder activar
        mision.AgregarEtapa(TipoModoDeJuego.Trivia, Guid.NewGuid());
        mision.Activar();

        _repositorio
            .Setup(r => r.ObtenerMisionPorIdAsync(mision.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mision);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarMisionComando(mision.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
        _repositorio.Verify(
            r => r.EliminarMisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
