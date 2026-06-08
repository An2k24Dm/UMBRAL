namespace SesionesServicio.Commons.Dtos;

public sealed class SesionDisponibleMovilDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public int CantidadMisiones { get; set; }

    // Solo se llenan cuando la sesión es Individual.
    public int? CantidadParticipantesActuales { get; set; }
    public int? CapacidadMaximaParticipantes { get; set; }

    // Solo se llenan cuando la sesión es Grupal.
    public int? CantidadEquiposActuales { get; set; }
    public int? CapacidadMaximaEquipos { get; set; }
}
