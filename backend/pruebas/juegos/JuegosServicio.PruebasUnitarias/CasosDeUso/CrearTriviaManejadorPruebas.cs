using JuegosServicio.Aplicacion.Comandos.CrearTrivia;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU15: pruebas del manejador de creación de trivia.
public class CrearTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private readonly Mock<IRegistroLogsAplicacion> _registro = new();
    private readonly Mock<IValidador<CrearTriviaComando>> _validador = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private CrearTriviaManejador CrearManejador() =>
        new(_repositorio.Object, _reloj.Object, _validador.Object, _registro.Object);

    private CrearTriviaComando ComandoValido(string nombre = "Trivia de Geografía") =>
        new(new CrearTriviaDto
        {
            Nombre = nombre,
            Descripcion = "Preguntas sobre capitales del mundo",
            TiempoLimitePorPregunta = 30
        }, Guid.NewGuid());

    public CrearTriviaManejadorPruebas()
    {
        _validador.Setup(v => v.Validar(It.IsAny<CrearTriviaComando>()))
                  .Returns(ResultadoValidacion.Exitoso());
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(FechaFija);
        _repositorio
            .Setup(r => r.ExisteTriviaConNombreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.AgregarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_NombreNuevo_RetornaIdNoVacio()
    {
        var resultado = await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        resultado.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NombreNuevo_LlamaAgregarTriviaAsyncUnaVez()
    {
        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        _repositorio.Verify(
            r => r.AgregarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NombreNuevo_UsaCreadorIdDelComando()
    {
        var creadorId = Guid.NewGuid();
        var comando = new CrearTriviaComando(
            new CrearTriviaDto
            {
                Nombre = "Trivia válida",
                Descripcion = "Descripción",
                TiempoLimitePorPregunta = 30
            }, creadorId);

        Trivia? triviaGuardada = null;
        _repositorio
            .Setup(r => r.AgregarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Callback<Trivia, CancellationToken>((t, _) => triviaGuardada = t)
            .Returns(Task.CompletedTask);

        await CrearManejador().Handle(comando, CancellationToken.None);

        triviaGuardada!.CreadorId.Should().Be(creadorId);
    }

    [Fact]
    public async Task Handle_NombreNuevo_UsaFechaDelReloj()
    {
        Trivia? triviaGuardada = null;
        _repositorio
            .Setup(r => r.AgregarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()))
            .Callback<Trivia, CancellationToken>((t, _) => triviaGuardada = t)
            .Returns(Task.CompletedTask);

        await CrearManejador().Handle(ComandoValido(), CancellationToken.None);

        triviaGuardada!.FechaCreacion.Should().Be(FechaFija);
    }

    [Fact]
    public async Task Handle_NombreDuplicado_LanzaExcepcionDominio()
    {
        _repositorio
            .Setup(r => r.ExisteTriviaConNombreAsync("Trivia existente", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accion = async () =>
            await CrearManejador().Handle(ComandoValido("Trivia existente"), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionDominio>();
    }

    [Fact]
    public async Task Handle_NombreDuplicado_NoLlamaAgregarTriviaAsync()
    {
        _repositorio
            .Setup(r => r.ExisteTriviaConNombreAsync("Trivia existente", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        try { await CrearManejador().Handle(ComandoValido("Trivia existente"), CancellationToken.None); }
        catch (ExcepcionDominio) { }

        _repositorio.Verify(
            r => r.AgregarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
