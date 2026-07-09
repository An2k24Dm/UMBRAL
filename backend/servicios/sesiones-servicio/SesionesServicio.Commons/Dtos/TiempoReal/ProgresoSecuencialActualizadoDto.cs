namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class ProgresoSecuencialActualizadoDto
{
    public Guid SesionId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public Guid? EquipoId { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
