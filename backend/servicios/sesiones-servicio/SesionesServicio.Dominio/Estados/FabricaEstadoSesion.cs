using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Estados;

// Devuelve la implementación de IEstadoSesion correspondiente al valor
// actual del enum EstadoSesion. Los estados son sin estado (stateless)
// y reentrantes, por eso se cachean como instancias estáticas.
public static class FabricaEstadoSesion
{
    private static readonly IEstadoSesion Programada = new EstadoSesionProgramada();
    private static readonly IEstadoSesion EnPreparacion = new EstadoSesionEnPreparacion();
    private static readonly IEstadoSesion Activa = new EstadoSesionActiva();
    private static readonly IEstadoSesion Pausada = new EstadoSesionPausada();
    private static readonly IEstadoSesion Finalizada = new EstadoSesionFinalizada();
    private static readonly IEstadoSesion Cancelada = new EstadoSesionCancelada();

    public static IEstadoSesion Obtener(EstadoSesion estado) => estado switch
    {
        EstadoSesion.Programada => Programada,
        EstadoSesion.EnPreparacion => EnPreparacion,
        EstadoSesion.Activa => Activa,
        EstadoSesion.Pausada => Pausada,
        EstadoSesion.Finalizada => Finalizada,
        EstadoSesion.Cancelada => Cancelada,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado de sesión no soportado.")
    };
}
