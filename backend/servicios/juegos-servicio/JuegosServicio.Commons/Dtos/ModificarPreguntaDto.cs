namespace JuegosServicio.Commons.Dtos;

public sealed class ModificarPreguntaDto
{
    public string NuevoEnunciado { get; set; } = default!;
    public int NuevoTiempoEstimado { get; set; } = 10;
    public List<OpcionDto> NuevasOpciones { get; set; } = new();
}
