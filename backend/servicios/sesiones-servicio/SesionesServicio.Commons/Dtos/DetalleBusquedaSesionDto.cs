namespace SesionesServicio.Commons.Dtos;

// HU34 — Snapshot del contenido de una Búsqueda del Tesoro para mostrar
// en el detalle de una sesión. Es una copia liviana de
// BusquedaTesoroDetalleDto de juegos-servicio para no acoplar Commons a
// JuegosServicio.Commons.
public sealed class DetalleBusquedaSesionDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public List<EtapaBusquedaSesionDto> Etapas { get; set; } = new();
}

public sealed class EtapaBusquedaSesionDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<PistaBusquedaSesionDto> Pistas { get; set; } = new();
}

public sealed class PistaBusquedaSesionDto
{
    public Guid Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Orden { get; set; }
}
