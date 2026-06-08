namespace JuegosServicio.Infraestructura.Persistencia.Modelos;

public sealed class EventoSalidaModelo
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = default!;
    public string Datos { get; set; } = default!;
    public DateTime FechaCreacion { get; set; }
    public bool Procesado { get; set; }
}
