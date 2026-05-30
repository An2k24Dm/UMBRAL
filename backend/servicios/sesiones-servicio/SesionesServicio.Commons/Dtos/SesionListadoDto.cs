namespace SesionesServicio.Commons.Dtos;

public sealed class SesionListadoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoJuego { get; set; } = string.Empty;
    public Guid ContenidoJuegoId { get; set; }
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
}
