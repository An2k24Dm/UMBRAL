using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

public sealed class DatosCreacionUsuario
{
    public RolUsuario TipoUsuario { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Sexo { get; init; } = "Indefinido";
    public DateTime FechaNacimiento { get; init; }
    public DatosContactoDto DatosContacto { get; init; } = new();
    public string? Alias { get; init; }
    public static DateTime NormalizarFechaNacimiento(DateTime fecha) =>
        DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
}
