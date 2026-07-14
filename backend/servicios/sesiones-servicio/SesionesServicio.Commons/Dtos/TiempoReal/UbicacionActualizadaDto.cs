namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class UbicacionActualizadaDto
{
    public Guid SesionId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid? EquipoId { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
