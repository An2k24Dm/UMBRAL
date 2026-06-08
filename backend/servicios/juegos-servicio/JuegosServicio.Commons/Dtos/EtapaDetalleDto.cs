namespace JuegosServicio.Commons.Dtos;

public sealed class EtapaDetalleDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string TipoModoDeJuego { get; set; } = default!;
    public Guid ModoDeJuegoId { get; set; }
    public string NombreModoDeJuego { get; set; } = default!;
    public int TiempoEstimado { get; set; }
}
