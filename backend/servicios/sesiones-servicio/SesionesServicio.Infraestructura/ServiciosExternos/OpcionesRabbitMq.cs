namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class OpcionesRabbitMq
{
    public const string Seccion = "RabbitMq";

    public string Host { get; set; } = "rabbitmq";
    public int Puerto { get; set; } = 5672;
    public string Usuario { get; set; } = "umbral";
    public string Contrasena { get; set; } = "umbral123";
    public string Exchange { get; set; } = "umbral.eventos";
    public string ColaResultadosRanking { get; set; } = "sesiones.ranking.resultados";
}
