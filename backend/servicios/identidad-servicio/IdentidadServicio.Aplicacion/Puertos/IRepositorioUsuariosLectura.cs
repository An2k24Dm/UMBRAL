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

    // HU34 — Devuelve, de la lista dada, los identificadores que
    // corresponden a usuarios internos con rol Administrador. Se usa
    // desde sesiones-servicio para resolver la visibilidad por rol
    // sin guardar el rol del creador en la entidad Sesion.
    Task<IReadOnlyList<Guid>> FiltrarAdministradoresPorIdsAsync(
        IReadOnlyCollection<Guid> usuariosIds, CancellationToken cancelacion);
}
