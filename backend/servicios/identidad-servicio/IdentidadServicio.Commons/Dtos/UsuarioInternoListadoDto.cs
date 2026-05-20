namespace IdentidadServicio.Commons.Dtos;

// Fila de listado de un usuario interno (Operador o Administrador) para HU08.
// La columna "Código" del frontend se arma a partir de CodigoOperador o
// CodigoAdministrador según el rol.
public sealed class UsuarioInternoListadoDto
{
    public Guid Id { get; init; }
    public string? CodigoOperador { get; init; }
    public string? CodigoAdministrador { get; init; }
    public string NombreUsuario { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Sexo { get; init; } = string.Empty;
}
