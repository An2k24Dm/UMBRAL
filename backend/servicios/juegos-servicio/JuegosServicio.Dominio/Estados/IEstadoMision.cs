namespace JuegosServicio.Dominio.Estados;

public interface IEstadoMision
{
    Enums.EstadoMision Estado { get; }
    void Activar(Entidades.Mision mision);
    void Desactivar(Entidades.Mision mision);
}
