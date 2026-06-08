namespace IdentidadServicio.Commons.Dtos;

// La modificación administrativa NO permite cambiar la contraseña: para eso
// existe el endpoint dedicado de reseteo de contraseña, que genera una
// contraseña temporal y la envía por correo al usuario.
public abstract class ModificarPerfilUsuarioDto
{
    public string? NombreUsuario { get; set; }
    public string? Correo { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Sexo { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public DatosContactoDto? DatosContacto { get; set; }
}
