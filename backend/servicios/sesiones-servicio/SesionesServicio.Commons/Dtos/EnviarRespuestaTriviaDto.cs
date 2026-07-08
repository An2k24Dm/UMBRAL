namespace SesionesServicio.Commons.Dtos;

public sealed class EnviarRespuestaTriviaDto
{
    public Guid PreguntaId { get; set; }
    public Guid OpcionSeleccionadaId { get; set; }
    public int TiempoTardadoMs { get; set; }
    public int TotalPreguntasEtapa { get; set; }
}
