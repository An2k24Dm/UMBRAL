namespace IdentidadServicio.Commons.Dtos;

// Perfil específico para usuarios con rol Operador.
// Hereda los datos comunes del DTO base y agrega únicamente el campo particular.
public sealed class PerfilOperadorDto : PerfilUsuarioDto
{
    public string CodigoOperador { get; set; } = string.Empty;
}
