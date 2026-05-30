using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Estados;

// Patrón State — comportamiento de Trivia cuando está Activa.
// Permite desactivarla y rechaza la activación (ya está activa).
// Bloquea la edición de preguntas.
internal sealed class EstadoTriviaActiva : IEstadoTrivia
{
    public EstadoTrivia Estado => EstadoTrivia.Activa;

    public void Activar(Entidades.Trivia trivia) =>
        throw new ExcepcionDominio("La trivia ya está activa.");

    public void Desactivar(Entidades.Trivia trivia)
    {
        trivia.TransicionarEstado(EstadoTrivia.Inactiva);
        trivia.AgregarEventoInterno(new TriviaArchivadaEvento(trivia.Id));
    }

    public void ValidarEdicion(string accion) =>
        throw new ExcepcionDominio($"No se pueden {accion} a una trivia que está activa.");
}
