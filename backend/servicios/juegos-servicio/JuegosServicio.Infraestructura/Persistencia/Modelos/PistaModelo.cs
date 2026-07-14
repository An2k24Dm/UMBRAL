namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class PistaModelo
{
    public Guid Id { get; set; }
    public Guid BusquedaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public int Tipo { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }

    public BusquedaTesoroModelo BusquedaTesoro { get; set; } = default!;
}
