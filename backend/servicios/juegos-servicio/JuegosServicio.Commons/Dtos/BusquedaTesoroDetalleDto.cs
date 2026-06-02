namespace JuegosServicio.Commons.Dtos;

public sealed class BusquedaTesoroDetalleDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public string Estado { get; set; } = default!;
    public DateTime FechaCreacion { get; set; }
    public MisionDetalleDto? Mision { get; set; }
}
