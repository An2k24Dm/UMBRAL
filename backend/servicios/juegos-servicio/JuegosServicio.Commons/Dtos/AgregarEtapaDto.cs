namespace JuegosServicio.Commons.Dtos;

public sealed class AgregarEtapaDto
{
    /// <summary>0 = Trivia, 1 = BusquedaTesoro</summary>
    public int TipoModoDeJuego { get; set; }
    public Guid ModoDeJuegoId { get; set; }
}
