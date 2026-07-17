namespace SesionesServicio.Commons.Dtos.ServiciosExternos;

public sealed class ParticipanteIdentidadResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}
