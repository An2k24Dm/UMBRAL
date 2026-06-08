namespace IdentidadServicio.Commons.Dtos;

// El Participante (HU10) edita su propio perfil desde la app móvil y SÍ
// puede cambiar su contraseña. Por eso este DTO añade los dos campos de
// contraseña aquí (NO en el DTO base ni en el DTO de Operador). De este
// modo el flujo administrativo de Operador/Administrador sigue sin recibir
// contraseña desde el frontend, pero el Participante conserva su flujo
// original móvil.
public sealed class ModificarParticipanteSolicitudDto : ModificarPerfilUsuarioDto
{
    public string? Alias { get; set; }
    public string? NuevaContrasena { get; set; }
    public string? ConfirmacionContrasena { get; set; }
}
