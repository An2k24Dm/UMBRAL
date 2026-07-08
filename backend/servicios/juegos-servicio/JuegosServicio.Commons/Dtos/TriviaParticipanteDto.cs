namespace JuegosServicio.Commons.Dtos;

public sealed class TriviaParticipanteDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int TiempoLimitePorPregunta { get; set; }
    public List<PreguntaParticipanteDto> Preguntas { get; set; } = new();
}

public sealed class PreguntaParticipanteDto
{
    public Guid Id { get; set; }
    public string Enunciado { get; set; } = default!;
    public int PuntajeAsignado { get; set; }
    public int TiempoEstimado { get; set; }
    public List<OpcionParticipanteDto> Opciones { get; set; } = new();
}

public sealed class OpcionParticipanteDto
{
    public Guid Id { get; set; }
    public string Texto { get; set; } = default!;
}
