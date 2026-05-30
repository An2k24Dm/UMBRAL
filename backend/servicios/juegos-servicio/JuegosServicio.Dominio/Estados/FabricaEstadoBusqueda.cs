using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Estados;

// Fábrica estática de estados de BusquedaTesoro (misma estrategia que
// FabricaEstadoTrivia y FabricaEstadoSesion en sesiones-servicio).
public static class FabricaEstadoBusqueda
{
    private static readonly IEstadoBusqueda Inactiva = new EstadoBusquedaInactiva();
    private static readonly IEstadoBusqueda Activa = new EstadoBusquedaActiva();

    public static IEstadoBusqueda Obtener(EstadoBusqueda estado) => estado switch
    {
        EstadoBusqueda.Inactiva => Inactiva,
        EstadoBusqueda.Activa => Activa,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado,
            "Estado de búsqueda no soportado.")
    };
}
