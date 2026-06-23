using JuegosServicio.Aplicacion.Comandos.ActivarTrivia;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU18: pruebas del manejador de activación de trivia.
public class ActivarTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ActivarTriviaManejador CrearManejador() => new(_repositorio.Object);

    private static Trivia TriviaConPregunta()
    {
        var trivia = Trivia.Crear("Trivia Test", "Descripción", Guid.NewGuid(), 30, FechaFija);
        trivia.AgregarPregunta("¿Pregunta?", 10, 10, [("Sí", true), ("No", false)]);
        return trivia;
    }

    public ActivarTriviaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.ActivarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TriviaConPregunta_LlamaActivarTriviaAsyncUnaVez()
    {
        var trivia = TriviaConPregunta();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador()
            .Handle(new ActivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        _repositorio.Verify(
            r => r.ActivarTriviaAsync(trivia, It.IsAny<CancellationToken>()),
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
                .Handle(new ActivarTriviaComando(triviaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_TriviaSinPreguntas_LanzaExcepcionDominio()
    {
        var triviaVacia = Trivia.Crear("Trivia vacía", "Descripción", Guid.NewGuid(), 30, FechaFija);
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(triviaVacia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triviaVacia);

        var accion = async () =>
            await CrearManejador()
                .Handle(new ActivarTriviaComando(triviaVacia.Id, triviaVacia.CreadorId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
