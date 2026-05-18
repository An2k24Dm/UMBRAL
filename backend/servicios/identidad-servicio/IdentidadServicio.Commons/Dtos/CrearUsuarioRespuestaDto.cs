namespace IdentidadServicio.Commons.Dtos;

public sealed class CrearUsuarioRespuestaDto
{
    public Guid Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
