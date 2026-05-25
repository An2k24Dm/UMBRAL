namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class OpcionModelo
{
    public Guid Id { get; set; }
    public Guid PreguntaId { get; set; }
    public string Texto { get; set; } = default!;
    public bool EsCorrecta { get; set; }

    public PreguntaModelo? Pregunta { get; set; }
}
