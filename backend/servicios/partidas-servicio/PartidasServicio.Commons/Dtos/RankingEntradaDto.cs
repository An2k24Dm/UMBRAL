namespace PartidasServicio.Commons.Dtos;

public sealed class RankingEntradaDto
{
    public int Posicion { get; set; }
    public Guid? EquipoId { get; set; }
    public Guid? ParticipanteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int PuntajeTotal { get; set; }
    public long TiempoTotalMs { get; set; }
    public int RespuestasCorrectas { get; set; }
}
