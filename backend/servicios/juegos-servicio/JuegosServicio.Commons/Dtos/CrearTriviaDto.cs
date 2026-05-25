namespace JuegosServicio.Commons.Dtos;

public sealed class CrearTriviaDto
{
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int TiempoLimitePorPregunta { get; set; }
}
