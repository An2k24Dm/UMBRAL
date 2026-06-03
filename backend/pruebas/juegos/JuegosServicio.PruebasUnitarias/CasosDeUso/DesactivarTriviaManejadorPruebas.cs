using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.CasosDeUso.Manejadores;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.CasosDeUso;

// HU20 + regla "no desactivar si hay sesiones vigentes": pruebas del
// manejador. Cubren autorización implícita, bloqueo cuando sesiones-
// servicio reporta sesión vigente y persistencia normal cuando no la
// reporta.
public class DesactivarTriviaManejadorPruebas
{
    private readonly Mock<IRepositorioJuegos> _repositorio = new();
    private readonly Mock<IClienteSesiones> _clienteSesiones = new();

    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private DesactivarTriviaManejador CrearManejador() =>
        new(_repositorio.Object, _clienteSesiones.Object);

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

        // Por defecto, sesiones-servicio reporta que no hay sesiones
        // vigentes: las pruebas que necesiten el caso contrario lo
        // sobreescriben explícitamente.
        _clienteSesiones
            .Setup(c => c.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.Trivia, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task Handle_TriviaActiva_SinSesionesVigentes_ArchivaCorrectamente()
    {
        var trivia = TriviaActiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        await CrearManejador()
            .Handle(new DesactivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        _clienteSesiones.Verify(c => c.ExisteSesionVigentePorContenidoAsync(
            TipoJuego.Trivia, trivia.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(
            r => r.DesactivarTriviaAsync(trivia, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TriviaConSesionVigente_LanzaContenidoConSesionesVigentes_YNoPersiste()
    {
        var trivia = TriviaActiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);
        _clienteSesiones
            .Setup(c => c.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.Trivia, trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var accion = async () => await CrearManejador()
            .Handle(new DesactivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        await accion.Should().ThrowAsync<ContenidoConSesionesVigentesExcepcion>();

        _repositorio.Verify(
            r => r.DesactivarTriviaAsync(It.IsAny<Trivia>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // La trivia sigue Activa: la transición de estado no se ejecuta.
        trivia.Estado.Should().Be(EstadoTrivia.Activa);
    }

    [Fact]
    public async Task Handle_ConsultaSesiones_AntesDeDesactivar()
    {
        // Verifica el orden: primero se consulta a IClienteSesiones,
        // recién después se persiste. Si la consulta dice "vigente",
        // el repositorio nunca debería ejecutar DesactivarTriviaAsync.
        var trivia = TriviaActiva();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(trivia.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trivia);

        var ordenLlamadas = new List<string>();
        _clienteSesiones
            .Setup(c => c.ExisteSesionVigentePorContenidoAsync(
                TipoJuego.Trivia, trivia.Id, It.IsAny<CancellationToken>()))
            .Callback(() => ordenLlamadas.Add("cliente"))
            .ReturnsAsync(false);
        _repositorio
            .Setup(r => r.DesactivarTriviaAsync(trivia, It.IsAny<CancellationToken>()))
            .Callback(() => ordenLlamadas.Add("repositorio"))
            .Returns(Task.CompletedTask);

        await CrearManejador()
            .Handle(new DesactivarTriviaComando(trivia.Id, trivia.CreadorId), CancellationToken.None);

        ordenLlamadas.Should().Equal("cliente", "repositorio");
    }

    [Fact]
    public async Task Handle_TriviaInexistente_LanzaExcepcionNoEncontrado_YNoConsultaSesiones()
    {
        var triviaId = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerTriviaPorIdAsync(triviaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trivia?)null);

        var accion = async () =>
            await CrearManejador()
                .Handle(new DesactivarTriviaComando(triviaId, Guid.NewGuid()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionNoEncontrado>();
        _clienteSesiones.Verify(c => c.ExisteSesionVigentePorContenidoAsync(
            It.IsAny<TipoJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
