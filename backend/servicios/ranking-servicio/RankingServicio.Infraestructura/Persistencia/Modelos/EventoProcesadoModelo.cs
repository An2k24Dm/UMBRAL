namespace RankingServicio.Infraestructura.Persistencia.Modelos;

public sealed class EventoProcesadoModelo
{
    public Guid Id { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public DateTime ProcesadoEnUtc { get; set; }
}
