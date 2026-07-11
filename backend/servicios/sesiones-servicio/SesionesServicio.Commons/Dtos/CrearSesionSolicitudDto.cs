namespace SesionesServicio.Commons.Dtos;

// Cuerpo del POST /api/sesiones. El cliente envía "Individual" o
// "Grupal" en `modo`; el backend lo traduce a la subclase concreta de
// Sesion (SesionIndividual / SesionGrupal) a través de la fábrica.
public sealed class CrearSesionSolicitudDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public List<Guid> MisionesIds { get; set; } = new();

    // Capacidad configurable por sesión. Solo se usan los campos que aplican
    // al modo: MaximoParticipantes para Individual; MaximoEquipos y
    // MaximoParticipantesPorEquipo para Grupal. Los demás se ignoran.
    public int? MaximoParticipantes { get; set; }
    public int? MaximoEquipos { get; set; }
    public int? MaximoParticipantesPorEquipo { get; set; }
    // Duración opcional en minutos. Si se indica, la sesión se finalizará automáticamente al vencer.
}
