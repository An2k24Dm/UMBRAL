namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class UsuarioModelo
{
    public Guid Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string IdKeycloak { get; set; } = string.Empty;
    public int Rol { get; set; }
    public int Estado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool DebeCambiarContrasena { get; set; }
    public PersonaModelo? Persona { get; set; }
}
