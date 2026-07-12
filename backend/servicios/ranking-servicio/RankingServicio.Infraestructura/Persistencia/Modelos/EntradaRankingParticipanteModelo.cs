namespace RankingServicio.Infraestructura.Persistencia.Modelos;

public sealed class EntradaRankingParticipanteModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public string NombreParticipante { get; set; } = string.Empty;
    public int PuntajeTotal { get; set; }
    public int RespuestasCorrectas { get; set; }
    public int RespuestasTotales { get; set; }
    public int EtapasCompletadas { get; set; }
    public int Posicion { get; set; }
    public DateTime UltimaActualizacionUtc { get; set; }
}
