namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class PreguntaModelo
{
    public Guid Id { get; set; }
    public Guid TriviaId { get; set; }
    public string Enunciado { get; set; } = default!;
    public int PuntajeAsignado { get; set; }
    public int TiempoEstimado { get; set; }

    public TriviaModelo? Trivia { get; set; }
    public List<OpcionModelo> Opciones { get; set; } = new();
}
