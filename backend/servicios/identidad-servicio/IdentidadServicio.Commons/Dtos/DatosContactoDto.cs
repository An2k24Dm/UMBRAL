namespace IdentidadServicio.Commons.Dtos;

// Datos de contacto secundarios (no incluye Correo — el Correo viaja como
// campo de primer nivel del usuario).
public sealed class DatosContactoDto
{
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
}
