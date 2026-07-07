namespace PartidasServicio.Infraestructura.Persistencia.Modelos;

public sealed class PartidaModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaInicioUtc { get; set; }
    public DateTime? FechaFinUtc { get; set; }
}
