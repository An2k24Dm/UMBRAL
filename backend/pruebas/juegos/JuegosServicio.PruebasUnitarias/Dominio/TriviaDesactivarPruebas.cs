using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU20: pruebas de Trivia.Desactivar (archivar).
public class TriviaDesactivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Trivia TriviaActiva()
    {
        var trivia = Trivia.Crear(
            "Trivia Test", "Descripción", Guid.NewGuid(), Tiempo.CrearPositivo(30), FechaFija);
        trivia.AgregarPregunta(
            "¿Pregunta?", Puntaje.CrearParaPregunta(10), Tiempo.CrearParaPregunta(10),
            [("Sí", true), ("No", false)]);
        trivia.Activar();
        return trivia;
    }

    [Fact]
    public void Desactivar_TriviaActiva_CambiaEstadoAArchivada()
    {
        var trivia = TriviaActiva();

        trivia.Desactivar();

        trivia.Estado.Should().Be(EstadoTrivia.Inactiva);
    }

    [Fact]
    public void Desactivar_TriviaYaInactiva_LanzaExcepcionDominio()
    {
        var trivia = Trivia.Crear(
            "Trivia Inactiva", "Descripción", Guid.NewGuid(), Tiempo.CrearPositivo(30), FechaFija);

        Action accion = () => trivia.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }

    [Fact]
    public void Desactivar_TriviaActiva_AgregaEventoTriviaArchivada()
    {
        var trivia = TriviaActiva();
        trivia.LimpiarEventos();

        trivia.Desactivar();

        trivia.Eventos.Should().ContainSingle(e => e is TriviaArchivadaEvento);
    }

    [Fact]
    public void Desactivar_TriviaYaInactivaSegundaVez_LanzaExcepcionDominio()
    {
        var trivia = TriviaActiva();
        trivia.Desactivar();

        Action accion = () => trivia.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }
}
