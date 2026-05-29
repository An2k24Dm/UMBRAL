namespace JuegosServicio.Dominio.Eventos;

public sealed class BusquedaArchivadaEvento : EventoDominio
{
    public Guid BusquedaId { get; }

    public BusquedaArchivadaEvento(Guid busquedaId)
    {
        BusquedaId = busquedaId;
    }
}
