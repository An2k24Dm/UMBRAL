namespace IdentidadServicio.Commons.Dtos;

public abstract class ModificarPerfilUsuarioDto
{
    public string? NombreUsuario { get; set; }
    public string? Correo { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Sexo { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public DatosContactoDto? DatosContacto { get; set; }
    public string? NuevaContrasena { get; set; }
    public string? ConfirmacionContrasena { get; set; }
}
