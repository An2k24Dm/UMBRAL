namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class TriviaModelo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public Guid CreadorId { get; set; }
    public int TiempoLimitePorPregunta { get; set; }
    public int Estado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public List<PreguntaModelo> Preguntas { get; set; } = new();
}
