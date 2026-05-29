using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Excepciones;

// Se lanza cuando se intenta una transición no permitida sobre una
// Sesion (por ejemplo, pausar una sesión Programada o reanudar una
// Finalizada). El mensaje detalla el estado origen y la acción para
// facilitar el diagnóstico en logs y respuestas del API.
public sealed class TransicionEstadoSesionInvalidaExcepcion : Exception
{
    public EstadoSesion EstadoOrigen { get; }
    public string Accion { get; }

    public TransicionEstadoSesionInvalidaExcepcion(EstadoSesion estadoOrigen, string accion)
        : base($"No se puede ejecutar '{accion}' desde el estado {estadoOrigen}.")
    {
        EstadoOrigen = estadoOrigen;
        Accion = accion;
    }
}
