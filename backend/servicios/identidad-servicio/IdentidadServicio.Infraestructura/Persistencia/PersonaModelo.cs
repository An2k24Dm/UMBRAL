namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class PersonaModelo
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public int Sexo { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public DateTime FechaRegistro { get; set; }

    public UsuarioModelo Usuario { get; set; } = null!;
    public AdministradorModelo? Administrador { get; set; }
    public OperadorModelo? Operador { get; set; }
    public ParticipanteModelo? Participante { get; set; }
}
