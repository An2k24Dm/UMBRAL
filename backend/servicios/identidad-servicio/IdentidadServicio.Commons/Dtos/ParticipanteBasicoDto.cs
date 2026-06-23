namespace IdentidadServicio.Commons.Dtos;

// HU43 — Datos básicos no sensibles de un participante. NO incluye correo,
// teléfono, dirección ni fecha de nacimiento.
public sealed class ParticipanteBasicoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}
