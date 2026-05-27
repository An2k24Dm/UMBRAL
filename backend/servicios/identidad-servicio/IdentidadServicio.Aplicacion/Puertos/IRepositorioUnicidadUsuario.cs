namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioUnicidadUsuario
{
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion);
    Task<bool> ExisteTelefonoAsync(string telefono, CancellationToken cancelacion);
    Task<bool> ExisteAliasAsync(string alias, CancellationToken cancelacion);
    Task<bool> ExisteNombreUsuarioEnOtroUsuarioAsync(
        string nombreUsuario, Guid idActual, CancellationToken cancelacion);
    Task<bool> ExisteCorreoEnOtroUsuarioAsync(
        string correo, Guid idActual, CancellationToken cancelacion);
    Task<bool> ExisteTelefonoEnOtroUsuarioAsync(
        string telefono, Guid idActual, CancellationToken cancelacion);
    Task<bool> ExisteAliasEnOtroUsuarioAsync(
        string alias, Guid idActual, CancellationToken cancelacion);
}
