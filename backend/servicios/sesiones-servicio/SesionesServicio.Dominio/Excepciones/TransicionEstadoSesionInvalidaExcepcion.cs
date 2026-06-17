using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Excepciones;

// Se lanza cuando se intenta una transición no permitida sobre una
// Sesion (por ejemplo, pausar una sesión Programada o reanudar una
// Finalizada). El mensaje específico de cada transición vive centralizado
// en MensajesTransicionEstadoSesion; los ConcreteState solo indican el
// estado origen y la acción.
public sealed class TransicionEstadoSesionInvalidaExcepcion : Exception
{
    public EstadoSesion EstadoOrigen { get; }
    public string Accion { get; }

    // Overload preferido por los ConcreteState: resuelve el mensaje desde la
    // clase centralizada a partir del estado origen y la acción.
    public TransicionEstadoSesionInvalidaExcepcion(EstadoSesion estadoOrigen, string accion)
        : this(
            estadoOrigen,
            accion,
            MensajesTransicionEstadoSesion.ObtenerMensaje(estadoOrigen, accion))
    {
    }

    public TransicionEstadoSesionInvalidaExcepcion(
        EstadoSesion estadoOrigen, string accion, string mensaje)
        : base(mensaje)
    {
        EstadoOrigen = estadoOrigen;
        Accion = accion;
    }
}
