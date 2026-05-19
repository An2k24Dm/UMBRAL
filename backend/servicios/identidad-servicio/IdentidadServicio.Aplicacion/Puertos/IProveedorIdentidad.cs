namespace IdentidadServicio.Aplicacion.Puertos;

public interface IProveedorIdentidad
{
    Task<ResultadoAutenticacionExterna?> IniciarSesionAsync(
        string nombreUsuario, string contrasena, CancellationToken cancelacion);

    // El proveedor recibe los datos necesarios para que Keycloak guarde no solo
    // username y email, sino también firstName y lastName. Se usa un record
    // dedicado para no convertir la firma en una lista larga de strings.
    Task<string> CrearUsuarioAsync(
        DatosCreacionUsuarioIdentidad datos,
        CancellationToken cancelacion);

    Task AsignarRolAsync(string idKeycloak, string nombreRol, CancellationToken cancelacion);

    Task EliminarUsuarioAsync(string idKeycloak, CancellationToken cancelacion);
}

// Datos mínimos que necesita el proveedor de identidad (Keycloak) para crear
// la cuenta. La contraseña viaja solo a Keycloak (nunca a PostgreSQL).
public sealed record DatosCreacionUsuarioIdentidad(
    string NombreUsuario,
    string Correo,
    string Contrasena,
    string Nombre,
    string Apellido);

public sealed record ResultadoAutenticacionExterna(
    string TokenAcceso,
    string TokenRefresco,
    int ExpiraEnSegundos,
    string TipoToken,
    string IdKeycloak);
