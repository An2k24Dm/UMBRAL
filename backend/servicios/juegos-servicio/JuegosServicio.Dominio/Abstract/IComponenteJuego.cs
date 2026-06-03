namespace JuegosServicio.Dominio.Abstract;

// Patrón Composite — contrato común para los modos de juego (hojas) y las
// etapas (nodos) de una Misión. Permite recorrer la jerarquía
// Misión → Etapa → (Trivia | BusquedaTesoro) de forma uniforme.
public interface IComponenteJuego
{
    Guid Id { get; }
    string ObtenerDescripcion();
    IReadOnlyList<IComponenteJuego> ObtenerHijos();
}
