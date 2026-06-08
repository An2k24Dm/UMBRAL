namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class MisionModelo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public Guid CreadorId { get; set; }
    public int Estado { get; set; }
    public int Dificultad { get; set; }
    public DateTime FechaCreacion { get; set; }

    public List<EtapaModelo> Etapas { get; set; } = new();
}
