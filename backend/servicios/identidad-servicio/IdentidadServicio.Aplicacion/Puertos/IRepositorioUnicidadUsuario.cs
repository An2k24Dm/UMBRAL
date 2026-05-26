namespace IdentidadServicio.Aplicacion.Puertos;

// Puerto dedicado a las consultas de unicidad usadas para detectar duplicados
// antes de persistir un usuario. Vive separado para que los manejadores que
// solo necesitan verificar duplicados no dependan de toda la API de lectura/
// escritura del repositorio.
//
// La distinción "EnOtroUsuario" es para los flujos de edición (HU09): el
// usuario actual puede mantener su valor sin que cuente como colisión.
public interface IRepositorioUnicidadUsuario
{
    // Alta (HU02 / HU03): el usuario aún no existe; cualquier coincidencia es
    // duplicado.
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion);
    Task<bool> ExisteTelefonoAsync(string telefono, CancellationToken cancelacion);

    // HU03 — alias del Participante. Único a nivel de base.
    Task<bool> ExisteAliasAsync(string alias, CancellationToken cancelacion);

    // Edición (HU09): se excluye el id del usuario que se está actualizando.
    Task<bool> ExisteNombreUsuarioEnOtroUsuarioAsync(
        string nombreUsuario, Guid idActual, CancellationToken cancelacion);
    Task<bool> ExisteCorreoEnOtroUsuarioAsync(
        string correo, Guid idActual, CancellationToken cancelacion);
    Task<bool> ExisteTelefonoEnOtroUsuarioAsync(
        string telefono, Guid idActual, CancellationToken cancelacion);
}
