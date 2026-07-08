namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class PistaLiberadaDto
{
    public Guid SesionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid? PistaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaEventoUtc { get; set; }
}
