using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU17: pruebas del manejador de eliminación de pregunta.
public class EliminarPreguntaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<ILogger<EliminarPreguntaManejador>> _registro = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private EliminarPreguntaManejador CrearManejador() =>
        new(_repositorio.Object, _registro.Object);

    private static Trivia TriviaConPregunta(out Guid preguntaId)
    {
        var trivia = Trivia.Crear("Trivia Test", "Descripción", Guid.NewGuid(), 30, FechaFija);
        var pregunta = trivia.AgregarPregunta(
            "Pregunta original", 10,
            [("Opción A", true), ("Opción B", false)]);
        preguntaId = pregunta.Id;
        return trivia;
    }

    public EliminarPreguntaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarPreguntaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TriviaYPreguntaExistentes_LlamaEliminarPreguntaAsyncUnaVez()
    {
        var trivia = TriviaConPregunta(out var preguntaId);
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador()
            .Handle(new EliminarPreguntaComando(trivia.Id, preguntaId), CancellationToken.None);

        _repositorio.Verify(
            r => r.EliminarPreguntaAsync(trivia.Id, preguntaId, It.IsAny<CancellationToken>()),
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
                .Handle(new EliminarPreguntaComando(triviaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_PreguntaInexistente_LanzaExcepcionNoEncontrado()
    {
        var trivia = TriviaConPregunta(out _);
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var accion = async () =>
            await CrearManejador()
                .Handle(new EliminarPreguntaComando(trivia.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_TriviaInexistente_NoLlamaEliminarPreguntaAsync()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trivia?)null);

        try
        {
            await CrearManejador()
                .Handle(new EliminarPreguntaComando(triviaId, Guid.NewGuid()), CancellationToken.None);
        }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.EliminarPreguntaAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
