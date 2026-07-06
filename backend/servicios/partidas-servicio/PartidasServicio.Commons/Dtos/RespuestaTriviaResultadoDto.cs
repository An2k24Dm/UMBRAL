namespace PartidasServicio.Commons.Dtos;

public sealed class RespuestaTriviaResultadoDto
{
    public bool EsCorrecta { get; set; }
    public int PuntosGanados { get; set; }
    public bool YaRespondida { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
