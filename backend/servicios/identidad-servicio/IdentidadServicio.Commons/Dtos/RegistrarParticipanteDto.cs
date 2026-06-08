namespace IdentidadServicio.Commons.Dtos;

public sealed class RegistrarParticipanteDto
{
    public string Alias { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Sexo { get; set; } = "Indefinido";
    public DateTime FechaNacimiento { get; set; }
    public DatosContactoDto DatosContacto { get; set; } = new();
}
