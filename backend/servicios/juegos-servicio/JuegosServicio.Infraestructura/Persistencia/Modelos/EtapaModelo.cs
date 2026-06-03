namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class EtapaModelo
{
    public Guid Id { get; set; }
    public Guid MisionId { get; set; }
    public int Orden { get; set; }
    public int TipoModoDeJuego { get; set; }
    public Guid ModoDeJuegoId { get; set; }

    public MisionModelo Mision { get; set; } = default!;
}
