namespace SesionesServicio.Commons.Dtos;

// Cuerpo del PUT /api/sesiones/{id}. Permite corregir los datos de una sesión
// en estado Programada. No incluye código de acceso ni estado: esos no se
// modifican. Si el cliente envía campos no soportados, se ignoran.
public sealed class ModificarSesionDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public List<Guid> MisionesIds { get; set; } = new();

    // Capacidad configurable. Solo se usan los campos que aplican al modo:
    // MaximoParticipantes para Individual; MaximoEquipos y
    // MaximoParticipantesPorEquipo para Grupal. Los demás se ignoran.
    public int? MaximoParticipantes { get; set; }
    public int? MaximoEquipos { get; set; }
    public int? MaximoParticipantesPorEquipo { get; set; }
}
