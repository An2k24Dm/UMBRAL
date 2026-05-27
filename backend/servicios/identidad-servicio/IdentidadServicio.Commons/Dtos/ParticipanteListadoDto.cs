namespace IdentidadServicio.Commons.Dtos;

public sealed class ParticipanteListadoDto
{
    public Guid Id { get; init; }
    public string? Alias { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Sexo { get; init; } = string.Empty;
}
