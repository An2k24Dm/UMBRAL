namespace IdentidadServicio.Aplicacion.Puertos;

public interface IProveedorIdentidad
{
    Task<ResultadoAutenticacionExterna?> IniciarSesionAsync(
        string nombreUsuario, string contrasena, CancellationToken cancelacion);

    // El proveedor recibe username y correo SEPARADOS y los envía igualmente
    // separados a Keycloak (username = nombreUsuario, email = correo).
    Task<string> CrearUsuarioAsync(
        string nombreUsuario, string correo, string contrasenaTemporal,
        CancellationToken cancelacion);

    Task AsignarRolAsync(string idKeycloak, string nombreRol, CancellationToken cancelacion);

    Task EliminarUsuarioAsync(string idKeycloak, CancellationToken cancelacion);
}

public sealed record ResultadoAutenticacionExterna(
    string TokenAcceso,
    string TokenRefresco,
    int ExpiraEnSegundos,
    string TipoToken,
    string IdKeycloak);
