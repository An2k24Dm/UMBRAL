using JuegosServicio.Aplicacion.Comandos.ModificarTrivia;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU19: pruebas del manejador de modificación de datos generales de trivia.
public class ModificarTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<IRepositorioMisiones> _repositorioMisiones = new();
    private readonly Mock<IValidador<ModificarTriviaComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private ModificarTriviaManejador CrearManejador() =>
        new(_repositorio.Object, _repositorioMisiones.Object, _validador.Object);

    private static Trivia TriviaEnBorrador() =>
        Trivia.Crear("Trivia Original", "Descripción original", Guid.NewGuid(), 30, FechaFija);

    private static ModificarTriviaComando ComandoValido(Guid triviaId) =>
        new(triviaId, new ModificarTriviaDto
        {
            NuevoNombre = "Trivia Modificada",
            NuevaDescripcion = "Nueva descripción",
            NuevoTiempoLimitePorPregunta = 45
        });

    public ModificarTriviaManejadorPruebas()
    {
        _repositorioMisiones.Setup(r => r.EsContenidoUsadoEnMisionActivaAsync(
            It.IsAny<TipoModoDeJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _validador.Setup(v => v.Validar(It.IsAny<ModificarTriviaComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
        _repositorio
            .Setup(r => r.ModificarDatosTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_TriviaExistente_LlamaModificarDatosTriviaAsyncUnaVez()
    {
        var trivia = TriviaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador().Handle(ComandoValido(trivia.Id), CancellationToken.None);

        _repositorio.Verify(
            r => r.ModificarDatosTriviaAsync(trivia, It.IsAny<CancellationToken>()),
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
    public async Task Handle_NombreVacio_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var comandoInvalido = new ModificarTriviaComando(trivia.Id, new ModificarTriviaDto
        {
            NuevoNombre = "",
            NuevaDescripcion = "Descripción",
            NuevoTiempoLimitePorPregunta = 30
        });

        var accion = async () =>
            await CrearManejador().Handle(comandoInvalido, CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }

    [Fact]
    public async Task Handle_TiempoLimiteCero_LanzaExcepcionDominio()
    {
        var trivia = TriviaEnBorrador();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var comandoInvalido = new ModificarTriviaComando(trivia.Id, new ModificarTriviaDto
        {
            NuevoNombre = "Nombre válido",
            NuevaDescripcion = "Descripción",
            NuevoTiempoLimitePorPregunta = 0
        });

        var accion = async () =>
            await CrearManejador().Handle(comandoInvalido, CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }
}
