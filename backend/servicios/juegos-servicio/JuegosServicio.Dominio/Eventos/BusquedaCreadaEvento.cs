namespace JuegosServicio.Dominio.Eventos;

public sealed class BusquedaCreadaEvento : EventoDominio
{
    public Guid BusquedaId { get; }
    public string Nombre { get; }

    public BusquedaCreadaEvento(Guid busquedaId, string nombre)
    {
        BusquedaId = busquedaId;
        Nombre = nombre;
    }
}
