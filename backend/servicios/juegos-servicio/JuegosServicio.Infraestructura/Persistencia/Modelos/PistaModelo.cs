namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class PistaModelo
{
    public Guid Id { get; set; }
    public Guid MisionId { get; set; }
    public string Contenido { get; set; } = default!;

    public MisionModelo Mision { get; set; } = default!;
}
