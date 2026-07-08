namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class EtapaCompletadaDto
{
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
