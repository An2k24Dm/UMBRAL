namespace JuegosServicio.Commons.Dtos;

public sealed class BusquedaTesoroParticipanteDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public int Tiempo { get; set; }
    public int Puntaje { get; set; }
    public List<PistaDetalleDto> Pistas { get; set; } = new();
}
