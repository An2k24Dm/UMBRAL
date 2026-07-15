namespace SesionesServicio.Commons.Dtos;

// DTO que el participante recibe: datos de la búsqueda + pistas ya liberadas en esta sesión/etapa.
public sealed class BusquedaTesoroConPistasDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int TiempoSegundos { get; set; }
    public int PuntajeBase { get; set; }
    public List<PistaLiberadaSesionDto> PistasLiberadas { get; set; } = new();
    public bool YaEnvioEvidencia { get; set; }
}

public sealed class PistaLiberadaSesionDto
{
    public Guid? PistaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public DateTime FechaLiberacionUtc { get; set; }
}
