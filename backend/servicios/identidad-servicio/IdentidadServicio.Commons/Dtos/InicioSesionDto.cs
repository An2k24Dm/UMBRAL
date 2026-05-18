namespace IdentidadServicio.Commons.Dtos;

public sealed class InicioSesionDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}
