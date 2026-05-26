namespace IdentidadServicio.Commons.Dtos;

// DTO base con los campos comunes de edición parcial de perfil de usuario.
// Lo extienden HU09 (Operador, editado por el Administrador) y HU10
// (Participante, edita su propio perfil desde la app móvil).
//
// Convención de edición parcial:
//  * null  → "este campo no cambia"; el manejador NO sobrescribe el actual.
//  * valor → se valida con las reglas comunes y se aplica al dominio.
//
// La contraseña jamás se persiste en PostgreSQL ni se devuelve en respuestas:
// viaja únicamente al endpoint reset-password de Keycloak.
public abstract class ModificarPerfilUsuarioDto
{
    public string? NombreUsuario { get; set; }
    public string? Correo { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Sexo { get; set; }
    public DateTime? FechaNacimiento { get; set; }

    // DatosContacto se acepta como objeto opcional. Si es null, no se toca
    // dirección ni teléfono. Si llega el objeto, cualquiera de sus dos campos
    // que venga en null se interpreta como "ese subcampo no cambia".
    public DatosContactoDto? DatosContacto { get; set; }

    // Sección Seguridad: cambio de contraseña administrativo (HU09) o por el
    // propio usuario (HU10). Ambos campos son opcionales; si ambos vienen
    // null, la contraseña no se modifica.
    public string? NuevaContrasena { get; set; }
    public string? ConfirmacionContrasena { get; set; }
}
