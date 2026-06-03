namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class PistaModelo
{
    public Guid Id { get; set; }
    public Guid BusquedaId { get; set; }
    public string Contenido { get; set; } = default!;

    public BusquedaTesoroModelo BusquedaTesoro { get; set; } = default!;
}
