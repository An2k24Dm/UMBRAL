namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class PistaModelo
{
    public Guid Id { get; set; }
    public Guid EtapaId { get; set; }
    public string Contenido { get; set; } = default!;

    public EtapaModelo Etapa { get; set; } = default!;
}
