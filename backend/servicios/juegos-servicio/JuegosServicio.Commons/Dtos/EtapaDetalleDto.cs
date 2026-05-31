namespace JuegosServicio.Commons.Dtos;

public sealed class EtapaDetalleDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int Orden { get; set; }
    public List<MisionDetalleDto> Misiones { get; set; } = new();
    public List<PistaDetalleDto> Pistas { get; set; } = new();
}
