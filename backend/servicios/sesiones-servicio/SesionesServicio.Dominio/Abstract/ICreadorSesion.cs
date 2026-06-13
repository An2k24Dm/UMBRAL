using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Dominio.Abstract;

// Strategy de creación de una sesión concreta. Cada modo de sesión
// (Individual, Grupal, ...) aporta su propio creador. Solo expone métodos:
// uno para declarar qué modo soporta y otro para construir el agregado.
// Agregar un nuevo tipo de sesión = agregar un nuevo ICreadorSesion, sin
// tocar la fábrica ni los manejadores.
public interface ICreadorSesion
{
    bool Soporta(string modo);

    Sesion Crear(
        string nombre,
        string descripcion,
        DateTime fechaProgramada,
        string codigoAcceso,
        Guid operadorCreadorId,
        DateTime fechaCreacionUtc);
}
