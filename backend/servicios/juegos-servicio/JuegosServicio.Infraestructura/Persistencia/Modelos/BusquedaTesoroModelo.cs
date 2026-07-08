namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class BusquedaTesoroModelo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public Guid CreadorId { get; set; }
    public int Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int Tiempo { get; set; }
    public int Puntaje { get; set; }
    public string CodigoQr { get; set; } = default!;

    public List<PistaModelo> Pistas { get; set; } = new();
}
