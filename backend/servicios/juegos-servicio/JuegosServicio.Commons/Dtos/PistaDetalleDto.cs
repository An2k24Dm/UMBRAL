namespace JuegosServicio.Commons.Dtos;

public sealed class PistaDetalleDto
{
    public Guid Id { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}
