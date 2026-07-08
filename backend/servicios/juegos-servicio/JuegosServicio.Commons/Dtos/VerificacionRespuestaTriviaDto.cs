namespace JuegosServicio.Commons.Dtos;

public sealed class VerificacionRespuestaTriviaDto
{
    public bool EsCorrecta { get; set; }
    public int PuntajeBase { get; set; }
    public int TiempoLimiteSegundos { get; set; }
}
