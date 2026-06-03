namespace JuegosServicio.Dominio.Abstract;

// Patrón State — contrato para cada estado posible de una Misión.
public interface IEstadoMision
{
    Enums.EstadoMision Estado { get; }
    void Activar(Entidades.Mision mision);
    void Desactivar(Entidades.Mision mision);
}
