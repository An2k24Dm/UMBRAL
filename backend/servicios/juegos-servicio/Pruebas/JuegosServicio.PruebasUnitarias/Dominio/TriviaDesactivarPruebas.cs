using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.PruebasUnitarias.Dominio;

// HU20: pruebas de Trivia.Desactivar (archivar).
public class TriviaDesactivarPruebas
{
    private static readonly DateTime FechaFija =
        new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Trivia TriviaActiva()
    {
        var trivia = Trivia.Crear("Trivia Test", "Descripción", Guid.NewGuid(), 30, FechaFija);
        trivia.AgregarPregunta("¿Pregunta?", 10, [("Sí", true), ("No", false)]);
        trivia.Activar();
        return trivia;
    }

    [Fact]
    public void Desactivar_TriviaActiva_CambiaEstadoAArchivada()
    {
        var trivia = TriviaActiva();

        trivia.Desactivar();

        trivia.Estado.Should().Be(EstadoTrivia.Archivada);
    }

    [Fact]
    public void Desactivar_TriviaEnBorrador_CambiaEstadoAArchivada()
    {
        var trivia = Trivia.Crear("Trivia Borrador", "Descripción", Guid.NewGuid(), 30, FechaFija);

        trivia.Desactivar();

        trivia.Estado.Should().Be(EstadoTrivia.Archivada);
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
    public void Desactivar_TriviaYaArchivada_LanzaExcepcionDominio()
    {
        var trivia = TriviaActiva();
        trivia.Desactivar();

        Action accion = () => trivia.Desactivar();

        accion.Should().Throw<ExcepcionDominio>();
    }
}
