using JuegosServicio.Aplicacion.Comandos.EliminarTrivia;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

public class EliminarTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private EliminarTriviaManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object);

    private static Trivia TriviaInactiva() =>
        Trivia.Crear(
            "Trivia Test", "Descripción", Guid.NewGuid(), Tiempo.CrearPositivo(30), FechaFija);

    public EliminarTriviaManejadorPruebas()
    {
        _repositorio
            .Setup(r => r.EliminarTriviaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositorioMisiones
            .Setup(r => r.EsContenidoUsadoEnEtapaAsync(
                TipoModoDeJuego.Trivia, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_TriviaInactivaSinMisiones_EliminaCorrectamente()
    {
        var trivia = TriviaInactiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador().Handle(new EliminarTriviaComando(trivia.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.EliminarTriviaAsync(trivia.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TriviaInexistente_LanzaExcepcionNoEncontrado()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trivia?)null);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarTriviaComando(triviaId), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
        _repositorio.Verify(
            r => r.EliminarTriviaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_TriviaActiva_LanzaExcepcionDominio()
    {
        var trivia = TriviaInactiva();
        trivia.AgregarPregunta(
            "¿Pregunta?", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(10),
        [
            ("Opción A", true),
            ("Opción B", false)
        ]);
        trivia.Activar();

        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarTriviaComando(trivia.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
        _repositorio.Verify(
            r => r.EliminarTriviaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_TriviaUsadaEnMision_LanzaExcepcionDominio()
    {
        var trivia = TriviaInactiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);
        _repositorioMisiones
            .Setup(r => r.EsContenidoUsadoEnEtapaAsync(
                TipoModoDeJuego.Trivia, trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accion = async () => await CrearManejador()
            .Handle(new EliminarTriviaComando(trivia.Id), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>()
            .WithMessage("*misiones*");
        _repositorio.Verify(
            r => r.EliminarTriviaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
