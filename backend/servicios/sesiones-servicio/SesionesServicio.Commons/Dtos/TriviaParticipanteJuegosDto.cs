namespace SesionesServicio.Commons.Dtos;

public sealed class TriviaParticipanteJuegosDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int TiempoLimitePorPregunta { get; set; }
    public List<PreguntaParticipanteJuegosDto> Preguntas { get; set; } = new();
}

public sealed class PreguntaParticipanteJuegosDto
{
    public Guid Id { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public int PuntajeAsignado { get; set; }
    public int TiempoEstimado { get; set; }
    public List<OpcionParticipanteJuegosDto> Opciones { get; set; } = new();
}

public sealed class OpcionParticipanteJuegosDto
{
    public Guid Id { get; set; }
    public string Texto { get; set; } = string.Empty;
}

public sealed class VerificacionRespuestaJuegosDto
{
    public bool EsCorrecta { get; set; }
    public int PuntajeBase { get; set; }
    public int TiempoLimiteSegundos { get; set; }
}
