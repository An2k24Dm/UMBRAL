namespace RankingServicio.Infraestructura.Persistencia.Modelos;

public sealed class RankingGlobalParticipanteModelo
{
    public Guid Id { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public string NombreParticipante { get; set; } = string.Empty;
    public long PuntajeAcumulado { get; set; }
    public int SesionesJugadas { get; set; }
    public int EtapasCompletadasTotal { get; set; }
    public DateTime UltimaActualizacionUtc { get; set; }
}
