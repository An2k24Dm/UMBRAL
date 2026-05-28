namespace JuegosServicio.Commons.Dtos;

public sealed class ModificarPreguntaDto
{
    public string NuevoEnunciado { get; set; } = default!;
    public List<OpcionDto> NuevasOpciones { get; set; } = new();
}
