using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioIdentidad
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<Usuario?> ObtenerPorIdKeycloakAsync(string idKeycloak, CancellationToken cancelacion);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion);
    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion);

    // El idKeycloak vive sólo en infraestructura: el dominio no lo conoce.
    Task GuardarAdministradorAsync(
        Administrador administrador, string idKeycloak, CancellationToken cancelacion);
    Task GuardarOperadorAsync(
        Operador operador, string idKeycloak, CancellationToken cancelacion);
    Task GuardarParticipanteAsync(
        Participante participante, string idKeycloak, CancellationToken cancelacion);
}
