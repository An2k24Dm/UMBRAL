namespace IdentidadServicio.Commons.Dtos;

// Perfil específico para usuarios con rol Participante.
// Hereda los datos comunes del DTO base y agrega únicamente el campo particular.
public sealed class PerfilParticipanteDto : PerfilUsuarioDto
{
    public string Alias { get; set; } = string.Empty;
}
