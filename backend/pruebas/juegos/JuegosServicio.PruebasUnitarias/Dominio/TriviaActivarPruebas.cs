using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU18: pruebas de Trivia.Activar — transición de estado y eventos de dominio.
public class TriviaActivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly IEnumerable<(string, bool)> OpcionesValidas =
        [("París", true), ("Madrid", false)];

    private static Trivia TriviaConPregunta()
    {
        var trivia = Trivia.Crear("Trivia Test", "Descripción", Guid.NewGuid(), 30, FechaFija);
        trivia.AgregarPregunta("¿Capital de Francia?", 10, 10, OpcionesValidas);
        return trivia;
    }

    [Fact]
    public void Activar_TriviaEnBorradorConPregunta_CambiaEstadoAActiva()
    {
        var trivia = TriviaConPregunta();

        trivia.Activar();

        trivia.Estado.Should().Be(EstadoTrivia.Activa);
    }

    [Fact]
    public void Activar_TriviaEnBorradorConPregunta_AgregaEventoTriviaActivada()
    {
        var trivia = TriviaConPregunta();
        trivia.LimpiarEventos();

        trivia.Activar();

        trivia.Eventos.Should().ContainSingle(e => e is TriviaActivadaEvento);
    }

    [Fact]
    public void Activar_TriviaSinPreguntas_LanzaExcepcionDominio()
    {
        var trivia = Trivia.Crear("Trivia vacía", "Descripción", Guid.NewGuid(), 30, FechaFija);

        Action accion = () => trivia.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_TriviaYaActiva_LanzaExcepcionDominio()
    {
        var trivia = TriviaConPregunta();
        trivia.Activar();

        Action accion = () => trivia.Activar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Activar_TriviaArchivada_CambiaEstadoAActiva()
    {
        var trivia = TriviaConPregunta();
        trivia.Activar();
        trivia.Desactivar();

        trivia.Activar();

        trivia.Estado.Should().Be(EstadoTrivia.Activa);
    }
}
