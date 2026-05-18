namespace IdentidadServicio.Commons.Dtos;

// DTO único para crear cualquier tipo de usuario.
// El frontend envía NombreUsuario y Correo separados:
//   - NombreUsuario  → username de Keycloak  (p. ej. "operador01")
//   - Correo         → email de Keycloak    (p. ej. "operador@umbral.com")
// El TipoUsuario selecciona la estrategia (Strategy + Factory).
public sealed class CrearUsuarioDto
{
    public TipoUsuario TipoUsuario { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string ContrasenaTemporal { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    // Sexo viaja como string ("Masculino" | "Femenino" | "Indefinido" | "Otro")
    // y se convierte al enum del dominio en la estrategia.
    public string Sexo { get; set; } = "Indefinido";
    public DateTime FechaNacimiento { get; set; }

    public DatosContactoDto DatosContacto { get; set; } = new();

    // Campos opcionales según TipoUsuario.
    public string? CodigoAdministrador { get; set; }
    public string? CodigoOperador { get; set; }
    public string? Alias { get; set; }
}
