namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class PistaLiberadaDto
{
    public Guid SesionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid? PistaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
