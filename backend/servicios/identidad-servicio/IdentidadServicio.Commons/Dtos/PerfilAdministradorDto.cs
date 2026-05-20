namespace IdentidadServicio.Commons.Dtos;

// Perfil específico para usuarios con rol Administrador.
// Hereda los datos comunes del DTO base y agrega únicamente el campo particular.
public sealed class PerfilAdministradorDto : PerfilUsuarioDto
{
    public string? CodigoAdministrador { get; set; }
}
