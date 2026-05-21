using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioIdentidad
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<Usuario?> ObtenerPorIdKeycloakAsync(string idKeycloak, CancellationToken cancelacion);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion);
    Task<bool> ExisteTelefonoAsync(string telefono, CancellationToken cancelacion);
    // HU03 — el alias del Participante es único. Se consulta antes de crear
    // en Keycloak para evitar tener que compensar por duplicados de alias.
    Task<bool> ExisteAliasAsync(string alias, CancellationToken cancelacion);

    // Devuelven el último código existente (mayor sufijo numérico) o null si
    // todavía no hay ninguno. Lo usa el GeneradorCodigoUsuario para calcular
    // el siguiente correlativo.
    Task<string?> ObtenerUltimoCodigoOperadorAsync(CancellationToken cancelacion);
    Task<string?> ObtenerUltimoCodigoAdministradorAsync(CancellationToken cancelacion);

    // El idKeycloak vive sólo en infraestructura: el dominio no lo conoce.
    Task GuardarAdministradorAsync(
        Administrador administrador, string idKeycloak, CancellationToken cancelacion);
    Task GuardarOperadorAsync(
        Operador operador, string idKeycloak, CancellationToken cancelacion);
    Task GuardarParticipanteAsync(
        Participante participante, string idKeycloak, CancellationToken cancelacion);

    // HU07: listado paginado de Participantes. La exclusión de Operadores y
    // Administradores ocurre en la implementación de infraestructura.
    Task<IReadOnlyList<Participante>> ConsultarParticipantesAsync(
        int pagina, int tamanioPagina, string? ordenEstado, CancellationToken cancelacion);
    Task<int> ContarParticipantesAsync(CancellationToken cancelacion);

    // HU07: detalle de un Participante por su Id. Devuelve null si el id no
    // existe o si corresponde a un usuario interno (Operador/Administrador).
    Task<Participante?> ObtenerParticipantePorIdAsync(
        Guid id, CancellationToken cancelacion);
    // HU08 — consulta paginada de cuentas internas (Operador / Administrador).
    // El filtro de rol es null para "Todos". El orden por estado puede ser
    // "asc", "desc" o null (sin orden explícito; se ordena por nombre).
    // Devuelve entidades de dominio concretas (Operador / Administrador); el
    // armado del DTO de listado corresponde al manejador del caso de uso.
    Task<IReadOnlyList<Usuario>> ConsultarUsuariosInternosAsync(
        int pagina,
        int tamanioPagina,
        RolUsuario? rolFiltro,
        string? ordenEstado,
        CancellationToken cancelacion);

    // HU08 — total de cuentas internas para la paginación. Respeta el mismo
    // filtro de rol que ConsultarUsuariosInternosAsync y nunca cuenta
    // Participantes.
    Task<int> ContarUsuariosInternosAsync(
        RolUsuario? rolFiltro,
        CancellationToken cancelacion);

    // HU08 — detalle de un usuario interno por id. Devuelve null cuando no
    // existe el id O cuando el usuario es Participante (no es interno).
    Task<Usuario?> ObtenerUsuarioInternoPorIdAsync(Guid id, CancellationToken cancelacion);
}
