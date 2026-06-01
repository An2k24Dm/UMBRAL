namespace SesionesServicio.Commons.Dtos;

// HU34 — Snapshot del contenido de una Trivia para mostrar en el
// detalle de una sesión. Es una copia liviana de TriviaDetalleDto de
// juegos-servicio para no acoplar Commons a JuegosServicio.Commons.
public sealed class DetalleTriviaSesionDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public List<PreguntaTriviaSesionDto> Preguntas { get; set; } = new();
}

public sealed class PreguntaTriviaSesionDto
{
    public Guid Id { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public int PuntajeAsignado { get; set; }
    public List<OpcionTriviaSesionDto> Opciones { get; set; } = new();
}

public sealed class OpcionTriviaSesionDto
{
    public Guid Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool EsCorrecta { get; set; }
}
