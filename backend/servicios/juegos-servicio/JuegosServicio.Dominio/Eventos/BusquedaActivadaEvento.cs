namespace JuegosServicio.Dominio.Eventos;

public sealed class BusquedaActivadaEvento : EventoDominio
{
    public Guid BusquedaId { get; }
    public string Nombre { get; }

    public BusquedaActivadaEvento(Guid busquedaId, string nombre)
    {
        BusquedaId = busquedaId;
        Nombre = nombre;
    }
}
