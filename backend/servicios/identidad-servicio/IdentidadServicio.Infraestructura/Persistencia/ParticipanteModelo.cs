namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class ParticipanteModelo
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public PersonaModelo Persona { get; set; } = null!;
}
