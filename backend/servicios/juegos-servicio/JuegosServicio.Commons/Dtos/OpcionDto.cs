namespace JuegosServicio.Commons.Dtos;

public sealed class OpcionDto
{
    public string Texto { get; set; } = default!;
    public bool EsCorrecta { get; set; }
}
