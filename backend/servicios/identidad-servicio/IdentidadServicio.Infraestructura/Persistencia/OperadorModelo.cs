namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class OperadorModelo
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public string CodigoOperador { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public PersonaModelo Persona { get; set; } = null!;
}
