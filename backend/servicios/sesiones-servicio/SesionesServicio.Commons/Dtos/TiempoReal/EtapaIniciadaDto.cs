namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class EtapaIniciadaDto
{
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public string TipoEtapa { get; set; } = string.Empty;
    public Guid ModoDeJuegoId { get; set; }
    public int OrdenGlobal { get; set; }
    public DateTime FechaInicioEtapaUtc { get; set; }
    public int DuracionSegundos { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
