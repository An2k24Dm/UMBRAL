namespace JuegosServicio.Dominio.Eventos;

public sealed class BusquedaActivadaEvento : EventoDominio
{
    public Guid BusquedaId { get; }
    public string Nombre { get; }
    public int CantidadEtapas { get; }

    public BusquedaActivadaEvento(Guid busquedaId, string nombre, int cantidadEtapas)
    {
        BusquedaId = busquedaId;
        Nombre = nombre;
        CantidadEtapas = cantidadEtapas;
    }
}
