namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class AdministradorModelo
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public string? CodigoAdministrador { get; set; }
    public DateTime FechaRegistro { get; set; }

    public PersonaModelo Persona { get; set; } = null!;
}
