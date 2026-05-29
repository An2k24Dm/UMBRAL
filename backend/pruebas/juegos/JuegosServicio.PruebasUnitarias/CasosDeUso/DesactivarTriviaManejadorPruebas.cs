using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU20: pruebas del manejador de desactivación (archivado) de trivia.
public class DesactivarTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private DesactivarTriviaManejador CrearManejador() => new(_repositorio.Object);

    private static Trivia TriviaActiva()
    {
        var trivia = Trivia.Crear("Trivia Test", "Descripción", Guid.NewGuid(), 30, FechaFija);
        trivia.AgregarPregunta("¿Pregunta?", 10, [("Sí", true), ("No", false)]);
        trivia.Activar();
        return trivia;
    }

    public DesactivarTriviaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ArchivarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TriviaActiva_LlamaArchivarTriviaAsyncUnaVez()
    {
        var trivia = TriviaActiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador()
            .Handle(new DesactivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        _repositorio.Verify(
            r => r.ArchivarTriviaAsync(trivia, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TriviaInexistente_LanzaExcepcionNoEncontrado()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trivia?)null);

        var accion = async () =>
            await CrearManejador()
                .Handle(new DesactivarTriviaComando(triviaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_TriviaYaArchivada_LanzaExcepcionDominio()
    {
        var trivia = TriviaActiva();
        trivia.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var accion = async () =>
            await CrearManejador()
                .Handle(new DesactivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
