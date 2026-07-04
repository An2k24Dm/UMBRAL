using JuegosServicio.Aplicacion.Comandos.AgregarPregunta;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU16: pruebas del manejador para agregar una pregunta a una trivia en borrador.
public class AgregarPreguntaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();
    private readonly Mock<ILogger<AgregarPreguntaManejador>> _registro = new();
    private readonly Mock<IValidador<AgregarPreguntaComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private AgregarPreguntaManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object, _validador.Object, _registro.Object);

    private static Trivia TriviaEnBorrador() => Trivia.Crear(
        "Trivia de Geografía", "Descripción", Guid.NewGuid(), Tiempo.CrearPositivo(30), FechaFija);

    private static AgregarPreguntaComando ComandoValido(Guid triviaId) =>
        new(triviaId, new AgregarPreguntaDto
        {
            Enunciado = "¿Capital de Francia?",
            PuntajeAsignado = 10,
            Opciones =
            [
                new OpcionDto { Texto = "París", EsCorrecta = true },
                new OpcionDto { Texto = "Madrid", EsCorrecta = false }
            ]
        });

    public AgregarPreguntaManejadorPruebas()
    {
        _repositorioMisiones.Setup(r => r.EsContenidoUsadoEnMisionActivaAsync(
            It.IsAny<TipoModoDeJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _validador.Setup(v => v.Validar(It.IsAny<AgregarPreguntaComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
        _repositorio
            .Setup(r => r.AgregarPreguntaAsync(
                It.IsAny<Guid>(), It.IsAny<Pregunta>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TriviaExistente_RetornaIdDePreguntaNoVacio()
    {
        var trivia = TriviaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var resultado = await CrearManejador().Handle(ComandoValido(trivia.Id), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_TriviaExistente_LlamaAgregarPreguntaAsyncUnaVez()
    {
        var trivia = TriviaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador().Handle(ComandoValido(trivia.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarPreguntaAsync(trivia.Id, It.IsAny<Pregunta>(), It.IsAny<CancellationToken>()),
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
            await CrearManejador().Handle(ComandoValido(triviaId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
    }

    [Fact]
    public async Task Handle_TriviaInexistente_NoLlamaAgregarPreguntaAsync()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trivia?)null);

        try { await CrearManejador().Handle(ComandoValido(triviaId), CancellationToken.None); }
        catch (ExcepcionNoEncontrado) { }

        _repositorio.Verify(
            r => r.AgregarPreguntaAsync(
                It.IsAny<Guid>(), It.IsAny<Pregunta>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PropagaTriviaIdAlRepositorioAlGuardar()
    {
        var trivia = TriviaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador().Handle(ComandoValido(trivia.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarPreguntaAsync(trivia.Id, It.IsAny<Pregunta>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
