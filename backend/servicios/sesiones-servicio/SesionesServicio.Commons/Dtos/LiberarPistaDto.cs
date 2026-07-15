namespace SesionesServicio.Commons.Dtos;

// PistaId null = pista personalizada; Contenido requerido si PistaId es null.
// Tipo: "Texto" (default) o "CoordenadaGps". Si GPS, Latitud y Longitud son requeridos.
public sealed class LiberarPistaDto
{
    public Guid? PistaId { get; set; }
    public string? Contenido { get; set; }
    public string Tipo { get; set; } = "Texto";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}
