namespace JuegosServicio.Dominio.Eventos;

public abstract class EventoDominio
{
    public Guid EventoId { get; } = Guid.NewGuid();
    public DateTime OcurridoEn { get; } = DateTime.UtcNow;
}
