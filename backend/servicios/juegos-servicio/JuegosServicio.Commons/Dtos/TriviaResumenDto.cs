namespace JuegosServicio.Commons.Dtos;

public sealed class TriviaResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int TiempoLimitePorPregunta { get; set; }
    public string Estado { get; set; } = default!;
    public int TotalPreguntas { get; set; }
    public DateTime FechaCreacion { get; set; }
}
