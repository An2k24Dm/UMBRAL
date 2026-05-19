namespace IdentidadServicio.Commons.Dtos;

public sealed class CrearUsuarioRespuestaDto
{
    public Guid Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    // Código generado en backend (OP-### para Operador, AD-### para Administrador).
    // Null para Participante.
    public string? Codigo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
