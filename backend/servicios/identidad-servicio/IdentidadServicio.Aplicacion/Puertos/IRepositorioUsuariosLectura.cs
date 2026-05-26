using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Puertos;

// Lecturas transversales sobre la jerarquía Usuario (cualquier rol) reutilizadas
// por autenticación (HU01), perfil del usuario autenticado (HU05/HU06) y
// listado/detalle de cuentas internas (HU08). Devuelve entidades de dominio.
public interface IRepositorioUsuariosLectura
{
    // HU01 — login: se busca por nombre de usuario tras autenticar en Keycloak.
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario, CancellationToken cancelacion);

    // HU05/HU06 — perfil del usuario autenticado: se llega con el sub de
    // Keycloak presente en el token.
    Task<Usuario?> ObtenerPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion);

    // HU08 — listado paginado de cuentas internas (Operador / Administrador).
    // El filtro de rol nulo equivale a "Todos". El orden por estado puede ser
    // "asc", "desc" o null. Nunca devuelve Participantes.
    Task<IReadOnlyList<Usuario>> ConsultarUsuariosInternosAsync(
        int pagina,
        int tamanioPagina,
        RolUsuario? rolFiltro,
        string? ordenEstado,
        CancellationToken cancelacion);

    Task<int> ContarUsuariosInternosAsync(
        RolUsuario? rolFiltro, CancellationToken cancelacion);

    // HU08 — detalle de una cuenta interna. Null si el id no existe o
    // corresponde a un Participante (no es interno).
    Task<Usuario?> ObtenerUsuarioInternoPorIdAsync(
        Guid id, CancellationToken cancelacion);
}
