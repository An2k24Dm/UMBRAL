using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — nodo: paso de una Misión que referencia un modo de juego
// (Trivia o BusquedaTesoro) por tipo e identificador.
public sealed class Etapa : IComponenteJuego
{
    public Guid Id { get; private set; }
    public Guid MisionId { get; private set; }
    public int Orden { get; private set; }
    public TipoModoDeJuego TipoModoDeJuego { get; private set; }
    public Guid ModoDeJuegoId { get; private set; }

    private Etapa() { }

    internal static Etapa Crear(
        Guid misionId,
        int orden,
        TipoModoDeJuego tipo,
        Guid modoDeJuegoId)
    {
        return new Etapa
        {
            Id = Guid.NewGuid(),
            MisionId = misionId,
            Orden = orden,
            TipoModoDeJuego = tipo,
            ModoDeJuegoId = modoDeJuegoId
        };
    }

    internal void ActualizarOrden(int nuevoOrden) => Orden = nuevoOrden;

    public static Etapa Reconstituir(
        Guid id,
        Guid misionId,
        int orden,
        TipoModoDeJuego tipo,
        Guid modoDeJuegoId) =>
        new() { Id = id, MisionId = misionId, Orden = orden, TipoModoDeJuego = tipo, ModoDeJuegoId = modoDeJuegoId };

    string IComponenteJuego.ObtenerDescripcion() =>
        $"Etapa {Orden}: {TipoModoDeJuego} [{ModoDeJuegoId}]";

    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Array.Empty<IComponenteJuego>();
}
