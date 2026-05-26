namespace JuegosServicio.Commons.Dtos;

public sealed class BusquedaTesoroResumenDto
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = default!;
    public string Descripcion { get; init; } = default!;
    public string Estado { get; init; } = default!;
    public int TotalEtapas { get; init; }
    public DateTime FechaCreacion { get; init; }
}
