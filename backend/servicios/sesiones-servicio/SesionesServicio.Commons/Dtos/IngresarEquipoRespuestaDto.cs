namespace SesionesServicio.Commons.Dtos;

// HU47 — Respuesta al ingresar a un equipo. Nunca incluye contraseña ni hash.
public sealed class IngresarEquipoRespuestaDto
{
    public Guid SesionId { get; set; }
    public Guid EquipoId { get; set; }
    public string EquipoNombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int CantidadParticipantes { get; set; }
    public int CapacidadMaxima { get; set; }
    public bool EsMiEquipo { get; set; } = true;
}
