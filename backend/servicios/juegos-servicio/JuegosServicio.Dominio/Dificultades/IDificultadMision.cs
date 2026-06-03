using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Dificultades;

// Strategy: cada implementación encapsula el comportamiento de un nivel
// de dificultad. Nuevos comportamientos (multiplicador de puntaje,
// color de UI, tiempo extra, etc.) se agregan aquí sin tocar la entidad.
public interface IDificultadMision
{
    NivelDificultad Nivel { get; }
    string Nombre { get; }
    string Descripcion { get; }
}
