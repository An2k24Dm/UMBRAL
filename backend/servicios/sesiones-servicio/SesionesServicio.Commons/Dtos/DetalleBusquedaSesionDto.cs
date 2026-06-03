namespace SesionesServicio.Commons.Dtos;

// HU34 — Snapshot del contenido de una Búsqueda del Tesoro para mostrar
// en el detalle de una sesión. Refleja la nueva estructura simplificada:
// BusquedaTesoro tiene sus pistas directamente, sin Mision intermediaria.
public sealed class DetalleBusquedaSesionDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public List<PistaBusquedaSesionDto> Pistas { get; set; } = new();
}

public sealed class PistaBusquedaSesionDto
{
    public Guid Id { get; set; }
    public string Contenido { get; set; } = string.Empty;
}
