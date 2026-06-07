using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioUsuariosLectura
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario, CancellationToken cancelacion);
    Task<Usuario?> ObtenerPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion);
    Task<IReadOnlyList<Usuario>> ConsultarUsuariosInternosAsync(
        int pagina,
        int tamanioPagina,
        RolUsuario? rolFiltro,
        string? ordenEstado,
        CancellationToken cancelacion);
    Task<int> ContarUsuariosInternosAsync(
        RolUsuario? rolFiltro, CancellationToken cancelacion);
    Task<Usuario?> ObtenerUsuarioInternoPorIdAsync(
        Guid id, CancellationToken cancelacion);

    Task<string?> ObtenerIdKeycloakUsuarioInternoAsync(
        Guid id, CancellationToken cancelacion);

    Task<IReadOnlyList<Guid>> FiltrarAdministradoresPorIdsAsync(
        IReadOnlyCollection<Guid> usuariosIds, CancellationToken cancelacion);
}
