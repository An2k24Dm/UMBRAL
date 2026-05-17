namespace IdentidadServicio.Commons.Dtos;

public sealed class PerfilUsuarioDto
{
    public Guid Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public DatosContactoDto DatosContacto { get; set; } = new();
    public string Sexo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
}
