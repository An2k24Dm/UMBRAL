namespace IdentidadServicio.Commons.Dtos;

// HU07: cada fila del listado de Participantes. No expone IdKeycloak ni
// códigos internos (CodigoOperador/CodigoAdministrador): esos no aplican a
// este rol.
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
