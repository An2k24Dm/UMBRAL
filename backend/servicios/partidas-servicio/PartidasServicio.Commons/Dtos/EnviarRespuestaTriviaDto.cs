namespace PartidasServicio.Commons.Dtos;

public sealed class EnviarRespuestaTriviaDto
{
    public Guid PreguntaId { get; set; }
    public Guid OpcionSeleccionadaId { get; set; }
    public long TiempoTardadoMs { get; set; }
}
