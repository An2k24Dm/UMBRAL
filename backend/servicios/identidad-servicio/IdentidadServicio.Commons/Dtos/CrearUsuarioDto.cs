using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Commons.Dtos;

// DTO único para crear cualquier tipo de usuario.
// El frontend envía NombreUsuario y Correo separados:
//   - NombreUsuario  → username de Keycloak  (p. ej. "operador01")
//   - Correo         → email de Keycloak    (p. ej. "operador@umbral.com")
// El TipoUsuario selecciona la estrategia (Strategy + Factory).
// Para evitar duplicar conceptos, el tipo se reutiliza desde el dominio
// (RolUsuario). Se conserva el nombre de propiedad "TipoUsuario" porque el
// frontend envía esa clave en el JSON.
//
// Nota HU02: los códigos OP-### / AD-### los genera el backend
// (IGeneradorCodigoUsuario). El frontend ya no los envía.
public sealed class CrearUsuarioDto
{
    public RolUsuario TipoUsuario { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    // Sexo viaja como string ("Masculino" | "Femenino" | "Indefinido" | "Otro")
    // y se convierte al enum del dominio en la estrategia.
    public string Sexo { get; set; } = "Indefinido";
    public DateTime FechaNacimiento { get; set; }

    public DatosContactoDto DatosContacto { get; set; } = new();

    // Alias se conserva para el registro futuro de Participante desde la app
    // móvil (HU03). No se usa en el flujo de registro web.
    public string? Alias { get; set; }
}
