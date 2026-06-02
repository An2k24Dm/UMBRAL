namespace JuegosServicio.Commons.Dtos;

public sealed class MisionDetalleDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public string Tipo { get; set; } = default!;
    public string PistaClave { get; set; } = default!;
    public List<PistaDetalleDto> Pistas { get; set; } = new();
}
