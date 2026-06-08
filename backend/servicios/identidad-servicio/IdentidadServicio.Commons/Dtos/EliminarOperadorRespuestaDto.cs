namespace IdentidadServicio.Commons.Dtos;

public sealed class EliminarOperadorRespuestaDto
{
    public Guid IdOperador { get; init; }
    public bool Eliminado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
