namespace SesionesServicio.Commons.Dtos;

public sealed class MisionConEtapasJuegosDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Dificultad { get; set; } = string.Empty;
    public List<EtapaJuegosDto> Etapas { get; set; } = new();
}

public sealed class EtapaJuegosDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string TipoModoDeJuego { get; set; } = string.Empty;
    public Guid ModoDeJuegoId { get; set; }
    public string NombreModoDeJuego { get; set; } = string.Empty;
    public int TiempoEstimado { get; set; }
}
