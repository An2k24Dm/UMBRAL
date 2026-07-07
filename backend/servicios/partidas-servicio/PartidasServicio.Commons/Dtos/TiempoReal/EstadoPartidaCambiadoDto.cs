namespace PartidasServicio.Commons.Dtos.TiempoReal;

public sealed class EstadoPartidaCambiadoDto
{
    public Guid SesionId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaEventoUtc { get; set; }
}
