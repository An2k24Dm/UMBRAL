namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class EtapaModelo
{
    public Guid Id { get; set; }
    public Guid BusquedaId { get; set; }
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int Orden { get; set; }

    public BusquedaTesoroModelo BusquedaTesoro { get; set; } = default!;
    public List<MisionModelo> Misiones { get; set; } = new();
}
