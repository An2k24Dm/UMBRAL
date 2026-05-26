namespace IdentidadServicio.Commons.Dtos;

// HU09 — DTO de actualización parcial del perfil de un Operador desde el panel
// web del Administrador.
//
// Todas las propiedades son opcionales (nullables). "null" significa
// explícitamente "este campo no cambia" y el manejador NO debe sobrescribir el
// valor actual en base de datos. Si se envía un valor, ese campo sí se
// actualiza y se valida con las mismas reglas usadas al crear.
//
// Esta HU no permite cambiar Estado, Rol, FechaRegistro, Id ni IdKeycloak:
// esos campos no se incluyen en el DTO a propósito. Cambiar el correo o el
// nombre de usuario también dispara la sincronización con Keycloak.
public sealed class ModificarOperadorSolicitudDto
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
}
