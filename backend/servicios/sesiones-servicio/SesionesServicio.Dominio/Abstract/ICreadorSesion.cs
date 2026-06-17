using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.Dominio.Abstract;

// Strategy de creación de una sesión concreta. Cada modo de sesión
// (Individual, Grupal, ...) aporta su propio creador. Solo expone métodos:
// uno para declarar qué modo soporta y otro para construir el agregado.
// Agregar un nuevo tipo de sesión = agregar un nuevo ICreadorSesion, sin
// tocar la fábrica ni los manejadores.
public interface ICreadorSesion
{
    bool Soporta(string modo);

    Sesion Crear(DatosCreacionSesion datos);

    // Reconstruye una sesión existente como este tipo, conservando su
    // identidad (Id, código de acceso, estado, fechas, operador). Se usa al
    // cambiar el modo de una sesión sin participantes ni equipos.
    Sesion Reconstruir(DatosReconstruccionSesion datos);
}
