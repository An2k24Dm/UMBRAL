namespace JuegosServicio.Dominio.Patrones;

// Patrón Composite — contrato común para todos los nodos del árbol de una
// Búsqueda del Tesoro: BusquedaTesoro (raíz), Mision (nodo compuesto) y
// Pista (hoja). Permite recorrer la jerarquía uniformemente sin
// distinguir entre compuestos y hojas.
public interface IComponenteJuego
{
    Guid Id { get; }
    string ObtenerDescripcion();
    IReadOnlyList<IComponenteJuego> ObtenerHijos();
}
