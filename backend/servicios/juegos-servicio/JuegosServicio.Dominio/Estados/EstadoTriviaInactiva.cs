using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Estados;

// Patrón State — comportamiento de Trivia cuando está Inactiva.
// Permite activarla y rechaza la desactivación (ya está inactiva).
// No restringe la edición de preguntas.
internal sealed class EstadoTriviaInactiva : IEstadoTrivia
{
    public EstadoTrivia Estado => EstadoTrivia.Inactiva;

    public void Activar(Entidades.Trivia trivia)
    {
        if (trivia.Preguntas.Count == 0)
            throw new ExcepcionDominio(
                "La trivia debe tener al menos una pregunta para poder activarse.");

        trivia.TransicionarEstado(EstadoTrivia.Activa);
        trivia.AgregarEventoInterno(
            new TriviaActivadaEvento(trivia.Id, trivia.Nombre, trivia.Preguntas.Count));
    }

    public void Desactivar(Entidades.Trivia trivia) =>
        throw new ExcepcionDominio("La trivia ya está inactiva.");

    public void ValidarEdicion(string accion) { /* permitido */ }
}
