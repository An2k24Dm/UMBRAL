namespace JuegosServicio.Dominio.Abstract;

// Patrón State — contrato para cada estado posible de una Trivia.
// Cada implementación encapsula las transiciones y validaciones
// válidas desde ese estado concreto, evitando bloques if/else en Trivia.
public interface IEstadoTrivia
{
    Enums.EstadoTrivia Estado { get; }
    void Activar(Entidades.Trivia trivia);
    void Desactivar(Entidades.Trivia trivia);
    void ValidarEdicion(string accion);
}
