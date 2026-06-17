namespace SesionesServicio.Commons.Dtos;

public sealed class SesionListadoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string CodigoAcceso { get; set; } = string.Empty;
    public Guid OperadorCreadorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int CantidadMisiones { get; set; }
    public int CantidadParticipantes { get; set; }
    public int CantidadEquipos { get; set; }

    // Capacidad configurada. Solo se llena la que aplica al modo:
    // MaximoParticipantes (Individual) o MaximoEquipos +
    // MaximoParticipantesPorEquipo (Grupal).
    public int? MaximoParticipantes { get; set; }
    public int? MaximoEquipos { get; set; }
    public int? MaximoParticipantesPorEquipo { get; set; }
}
