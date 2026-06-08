namespace JuegosServicio.Commons.Dtos;

public sealed class AgregarPreguntaDto
{
    public string Enunciado { get; set; } = default!;
    public int PuntajeAsignado { get; set; }
    public int TiempoEstimado { get; set; } = 10;
    public List<OpcionDto> Opciones { get; set; } = new();
}
