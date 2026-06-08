namespace JuegosServicio.Commons.Dtos;

public sealed class TriviaDetalleDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int TiempoLimitePorPregunta { get; set; }
    public string Estado { get; set; } = default!;
    public DateTime FechaCreacion { get; set; }
    public int PuntajeTotal { get; set; }
    public int TiempoTotal { get; set; }
    public List<PreguntaDetalleDto> Preguntas { get; set; } = new();
}
