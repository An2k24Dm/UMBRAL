namespace JuegosServicio.Commons.Dtos;

public sealed class CrearBusquedaTesoroDto
{
    public string Nombre { get; init; } = default!;
    public string Descripcion { get; init; } = default!;
    public int Tiempo { get; init; } = 0;
    public int Puntaje { get; init; } = 0;
}
