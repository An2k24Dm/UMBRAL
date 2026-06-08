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
    // Asigna una nueva contraseña al usuario en Keycloak. UMBRAL siempre
    // guarda la credencial como NO temporal: el control de "obligar a
    // cambiarla en el próximo login" se hace contra una bandera propia en
    // la BD de identidad, no contra Keycloak. Esto evita el rechazo de
    // Direct Access Grants ("Account is not fully set up") cuando hay
    // required actions pendientes.
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
