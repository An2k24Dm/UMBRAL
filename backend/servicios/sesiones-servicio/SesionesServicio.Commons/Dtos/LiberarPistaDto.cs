namespace SesionesServicio.Commons.Dtos;

// PistaId null = pista personalizada; Contenido requerido si PistaId es null.
public sealed class LiberarPistaDto
{
    public Guid? PistaId { get; set; }
    public string? Contenido { get; set; }
}
