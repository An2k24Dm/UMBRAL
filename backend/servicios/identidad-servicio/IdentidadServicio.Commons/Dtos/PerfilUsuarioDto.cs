namespace IdentidadServicio.Commons.Dtos;

// DTO base con los datos comunes del perfil. Se mantiene como clase abierta
// para que cada rol pueda derivar y agregar SOLO sus campos particulares
// (CodigoAdministrador, CodigoOperador, Alias). Esto respeta OCP: incorporar
// un nuevo rol no obliga a modificar este tipo.
public class PerfilUsuarioDto
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
    public DateTime FechaRegistro { get; set; }
}
