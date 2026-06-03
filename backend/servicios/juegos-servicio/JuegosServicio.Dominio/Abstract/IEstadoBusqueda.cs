namespace JuegosServicio.Dominio.Abstract;

// Patrón State — contrato para cada estado posible de una BusquedaTesoro.
public interface IEstadoBusqueda
{
    Enums.EstadoBusqueda Estado { get; }
    void Activar(Entidades.BusquedaTesoro busqueda);
    void Desactivar(Entidades.BusquedaTesoro busqueda);
    void ValidarEdicion(string accion);
}
