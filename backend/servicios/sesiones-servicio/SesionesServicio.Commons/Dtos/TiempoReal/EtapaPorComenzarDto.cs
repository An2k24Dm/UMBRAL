namespace SesionesServicio.Commons.Dtos.TiempoReal;

// Aviso de que la SIGUIENTE etapa ya está programada y comenzará en breve. NO
// significa que sea jugable todavía (eso lo indica EtapaIniciada). Alimenta el
// banner global de preparación del participante. Sin datos sensibles.
public sealed class EtapaPorComenzarDto
{
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public string TipoEtapa { get; set; } = string.Empty;
    public Guid ModoDeJuegoId { get; set; }
    public int NumeroMision { get; set; }
    public int NumeroEtapa { get; set; }
    public int OrdenGlobal { get; set; }
    public bool EsNuevaMision { get; set; }
    public DateTime FechaInicioProgramadaUtc { get; set; }
    public int DuracionPreparacionSegundos { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
