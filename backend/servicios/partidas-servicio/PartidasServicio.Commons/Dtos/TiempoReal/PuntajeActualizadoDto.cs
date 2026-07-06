namespace PartidasServicio.Commons.Dtos.TiempoReal;

public sealed class PuntajeActualizadoDto
{
    public Guid SesionId { get; set; }
    public DateTime FechaEventoUtc { get; set; }
    public IReadOnlyList<RankingEntradaDto> Ranking { get; set; } = [];
}
