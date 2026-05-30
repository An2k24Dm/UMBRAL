namespace JuegosServicio.Dominio.Patrones;

// Patrón Composite — contrato común para los nodos del árbol de una
// Búsqueda del Tesoro: BusquedaTesoro (raíz), Etapa (compuesto) y
// Mision (hoja). Permite recorrer la jerarquía uniformemente sin
// distinguir entre compuestos y hojas.
public interface IComponenteJuego
{
    Guid Id { get; }
    string ObtenerDescripcion();
    IReadOnlyList<IComponenteJuego> ObtenerHijos();
}
