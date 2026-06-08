namespace IdentidadServicio.Commons.Dtos;

public sealed class CambiarEstadoUsuarioRespuestaDto
{
    public Guid IdUsuario { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
}
