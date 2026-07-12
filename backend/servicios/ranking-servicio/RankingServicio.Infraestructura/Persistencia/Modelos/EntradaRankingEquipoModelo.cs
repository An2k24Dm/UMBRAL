namespace RankingServicio.Infraestructura.Persistencia.Modelos;

public sealed class EntradaRankingEquipoModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid EquipoId { get; set; }
    public string NombreEquipo { get; set; } = string.Empty;
    public int PuntajeTotal { get; set; }
    public int EtapasCompletadas { get; set; }
    public int Posicion { get; set; }
    public DateTime UltimaActualizacionUtc { get; set; }
}
