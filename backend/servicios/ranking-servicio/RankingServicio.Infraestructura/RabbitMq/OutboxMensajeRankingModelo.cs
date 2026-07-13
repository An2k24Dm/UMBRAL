namespace RankingServicio.Infraestructura.RabbitMq;

public sealed class OutboxMensajeRankingModelo
{
    public Guid Id { get; set; }
    public string RoutingKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreadoEnUtc { get; set; }
    public DateTime? EnviadoEnUtc { get; set; }
    public int Intentos { get; set; }
    public DateTime? ProximoIntentoUtc { get; set; }
    public string? UltimoError { get; set; }
    public string Estado { get; set; } = "Pendiente";
}
