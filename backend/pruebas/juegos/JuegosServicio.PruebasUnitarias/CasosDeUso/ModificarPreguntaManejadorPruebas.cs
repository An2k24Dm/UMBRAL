using JuegosServicio.Aplicacion.Comandos.ModificarPregunta;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU17: pruebas del manejador de modificación de pregunta.
public class ModificarPreguntaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();
    private readonly Mock<ILogger<ModificarPreguntaManejador>> _registro = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarPreguntaManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object, _registro.Object);

    private static Trivia TriviaConPregunta(out Guid preguntaId)
    {
        var trivia = Trivia.Crear(
            "Trivia Test", "Descripción", Guid.NewGuid(), Tiempo.CrearPositivo(30), FechaFija);
        var pregunta = trivia.AgregarPregunta(
            "Pregunta original", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(10),
            [("Opción A", true), ("Opción B", false)]);
        preguntaId = pregunta.Id;
        return trivia;
    }

    public ModificarPreguntaManejadorPruebas()
    {
        _repositorioMisiones.Setup(r => r.EsContenidoUsadoEnMisionActivaAsync(
            It.IsAny<TipoModoDeJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.ModificarPreguntaAsync(
                It.IsAny<Guid>(), It.IsAny<Pregunta>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static ModificarPreguntaComando ComandoValido(Guid triviaId, Guid preguntaId) =>
        new(triviaId, preguntaId, new ModificarPreguntaDto
        {
            NuevoEnunciado = "¿Capital de Francia?",
            NuevasOpciones =
            [
                new OpcionDto { Texto = "París", EsCorrecta = true },
                new OpcionDto { Texto = "Madrid", EsCorrecta = false }
            ]
        });

    [Fact]
    public async Task Handle_TriviaYPreguntaExistentes_LlamaModificarPreguntaAsyncUnaVez()
    {
        var trivia = TriviaConPregunta(out var preguntaId);
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador().Handle(ComandoValido(trivia.Id, preguntaId), CancellationToken.None);

        _repositorio.Verify(
            r => r.ModificarPreguntaAsync(trivia.Id, It.IsAny<Pregunta>(), It.IsAny<CancellationToken>()),
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
            await CrearManejador().Handle(ComandoValido(triviaId, Guid.NewGuid()), CancellationToken.None);

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
            await CrearManejador().Handle(
                ComandoValido(trivia.Id, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }
}
