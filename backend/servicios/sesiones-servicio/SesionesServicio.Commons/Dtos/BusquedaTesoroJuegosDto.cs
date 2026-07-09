namespace SesionesServicio.Commons.Dtos;

public sealed class BusquedaTesoroJuegosDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Tiempo { get; set; }
    public int Puntaje { get; set; }
    public List<PistaJuegosDto> Pistas { get; set; } = new();
}

public sealed class PistaJuegosDto
{
    public Guid Id { get; set; }
    public string Contenido { get; set; } = string.Empty;
}
