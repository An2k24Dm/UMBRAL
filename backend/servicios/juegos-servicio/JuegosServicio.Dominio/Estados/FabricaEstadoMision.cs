using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Estados;

internal static class FabricaEstadoMision
{
    public static IEstadoMision Obtener(EstadoMision estado) => estado switch
    {
        EstadoMision.Inactiva => new EstadoMisionInactiva(),
        EstadoMision.Activa   => new EstadoMisionActiva(),
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null)
    };
}
