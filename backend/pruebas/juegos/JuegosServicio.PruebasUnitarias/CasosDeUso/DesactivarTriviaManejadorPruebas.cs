using JuegosServicio.Aplicacion.Comandos.DesactivarTrivia;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// Pruebas del manejador de desactivación de trivia. Ya no hay
// validación cruzada con sesiones-servicio (endpoint viejo eliminado).
public class DesactivarTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private DesactivarTriviaManejador CrearManejador() =>
        new(_repositorio.Object);

    private static Trivia TriviaActiva()
    {
        var trivia = Trivia.Crear("Trivia Test", "Descripción", Guid.NewGuid(), 30, FechaFija);
        trivia.AgregarPregunta("¿Pregunta?", 10, 10, [("Sí", true), ("No", false)]);
        trivia.Activar();
        return trivia;
    }

    public DesactivarTriviaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.DesactivarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TriviaActiva_ArchivaYPersiste()
    {
        var trivia = TriviaActiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador()
            .Handle(new DesactivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        trivia.Estado.Should().Be(EstadoTrivia.Inactiva);
        _repositorio.Verify(
            r => r.DesactivarTriviaAsync(trivia, It.IsAny<CancellationToken>()),
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
