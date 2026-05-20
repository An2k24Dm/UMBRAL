using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioIdentidad
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<Usuario?> ObtenerPorIdKeycloakAsync(string idKeycloak, CancellationToken cancelacion);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion);
    Task<bool> ExisteTelefonoAsync(string telefono, CancellationToken cancelacion);

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
}
