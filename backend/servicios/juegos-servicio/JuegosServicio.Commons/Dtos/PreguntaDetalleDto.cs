namespace JuegosServicio.Commons.Dtos;

public sealed class PreguntaDetalleDto
{
    public Guid Id { get; set; }
    public string Enunciado { get; set; } = default!;
    public int PuntajeAsignado { get; set; }
    public int TiempoEstimado { get; set; }
    public List<OpcionDetalleDto> Opciones { get; set; } = new();
}

public sealed class OpcionDetalleDto
{
    public Guid Id { get; set; }
    public string Texto { get; set; } = default!;
    public bool EsCorrecta { get; set; }
}
