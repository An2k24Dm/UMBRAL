namespace IdentidadServicio.Aplicacion.Puertos;

public interface IProveedorIdentidad
{
    Task<ResultadoAutenticacionExterna?> IniciarSesionAsync(
        string nombreUsuario, string contrasena, CancellationToken cancelacion);
    Task<string> CrearUsuarioAsync(
        DatosCreacionUsuarioIdentidad datos,
        CancellationToken cancelacion);
    Task AsignarRolAsync(string idKeycloak, string nombreRol, CancellationToken cancelacion);
    Task EliminarUsuarioAsync(string idKeycloak, CancellationToken cancelacion);
    Task ActualizarUsuarioAsync(
        string idKeycloak,
        DatosActualizacionUsuarioIdentidad datos,
        CancellationToken cancelacion);
    Task CambiarContrasenaAsync(
        string idKeycloak,
        string nuevaContrasena,
        CancellationToken cancelacion);
}

public sealed record DatosCreacionUsuarioIdentidad(
    string NombreUsuario,
    string Correo,
    string Contrasena,
    string Nombre,
    string Apellido);

public sealed record DatosActualizacionUsuarioIdentidad(
    string? NombreUsuario,
    string? Correo,
    string? Nombre,
    string? Apellido)
{
    public bool TieneCambios =>
        NombreUsuario is not null || Correo is not null ||
        Nombre is not null || Apellido is not null;
}

public sealed record ResultadoAutenticacionExterna(
    string TokenAcceso,
    string TokenRefresco,
    int ExpiraEnSegundos,
    string TipoToken,
    string IdKeycloak);
