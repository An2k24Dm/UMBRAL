namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class MisionModelo
{
    public Guid Id { get; set; }
    public Guid BusquedaId { get; set; }
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int Tipo { get; set; }
    public string PistaClave { get; set; } = default!;

    public BusquedaTesoroModelo BusquedaTesoro { get; set; } = default!;
    public List<PistaModelo> Pistas { get; set; } = new();
}
