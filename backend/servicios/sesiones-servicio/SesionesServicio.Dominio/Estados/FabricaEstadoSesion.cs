using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Reconstruye el ConcreteState del patrón State a partir del valor del
// enum EstadoSesion guardado en persistencia. La usa Sesion.Crear para
// inicializar el estado y Sesion.Rehidratar para reconstruir el estado
// desde la base de datos.
public static class FabricaEstadoSesion
{
    public static IEstadoSesion Crear(EstadoSesion estado) => estado switch
    {
        EstadoSesion.Programada => new EstadoSesionProgramada(),
        EstadoSesion.EnPreparacion => new EstadoSesionEnPreparacion(),
        EstadoSesion.Activa => new EstadoSesionActiva(),
        EstadoSesion.Pausada => new EstadoSesionPausada(),
        EstadoSesion.Finalizada => new EstadoSesionFinalizada(),
        EstadoSesion.Cancelada => new EstadoSesionCancelada(),
        _ => throw new TransicionEstadoSesionInvalidaExcepcion(
            estado, "Crear",
            MensajesTransicionEstadoSesion.EstadoNoValido)
    };
}
