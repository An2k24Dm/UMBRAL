namespace RankingServicio.Dominio.Entidades;

public sealed class EventoProcesado
{
    public Guid Id { get; private set; }
    public string TipoEvento { get; private set; } = string.Empty;
    public DateTime ProcesadoEnUtc { get; private set; }

    private EventoProcesado() { }

    public static EventoProcesado Crear(Guid id, string tipoEvento, DateTime ahora)
        => new() { Id = id, TipoEvento = tipoEvento, ProcesadoEnUtc = ahora };
}
