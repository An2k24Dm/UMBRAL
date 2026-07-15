using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Excepciones;

public sealed class TransicionEstadoSesionInvalidaExcepcion : Exception
{
    public EstadoSesion EstadoOrigen { get; }
    public string Accion { get; }

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
