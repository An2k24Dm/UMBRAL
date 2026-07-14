namespace JuegosServicio.Commons.Dtos;

public sealed class AgregarPistaDto
{
    public string? Contenido { get; init; }
    public string Tipo { get; init; } = "Texto";
    public double? Latitud { get; init; }
    public double? Longitud { get; init; }
}
