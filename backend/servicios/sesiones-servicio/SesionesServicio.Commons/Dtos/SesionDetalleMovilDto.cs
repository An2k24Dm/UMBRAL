namespace SesionesServicio.Commons.Dtos;

public sealed class SesionDetalleMovilDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string CodigoAcceso { get; set; } = string.Empty;
    public List<MisionSesionMovilDto> Misiones { get; set; } = new();
}

public sealed class MisionSesionMovilDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Dificultad { get; set; }
    public int TotalEtapas { get; set; }
    public List<EtapaSesionMovilDto> Etapas { get; set; } = new();
}

public sealed class EtapaSesionMovilDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string TipoModoDeJuego { get; set; } = string.Empty;
    public string NombreModoDeJuego { get; set; } = string.Empty;
    public int? TiempoEstimadoSegundos { get; set; }
}
