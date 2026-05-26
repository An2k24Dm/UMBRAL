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

    // HU09 — actualización parcial en Keycloak. Sólo se envían los campos que
    // realmente cambiaron en backend (correo, nombre de usuario, nombre o
    // apellido). Pasar null en una propiedad significa "sin cambio". Si todos
    // son null, la implementación no debe llamar a Keycloak.
    Task ActualizarUsuarioAsync(
        string idKeycloak,
        DatosActualizacionUsuarioIdentidad datos,
        CancellationToken cancelacion);

    // HU09 — cambio de contraseña administrativo desde el panel del
    // Administrador. La contraseña NUNCA se persiste en PostgreSQL ni se
    // devuelve en ningún DTO: viaja directamente al endpoint
    // reset-password de Keycloak.
    //
    // `temporal` controla si Keycloak debe pedir al usuario que la cambie en
    // el próximo login (true) o aceptarla como definitiva (false). El caso de
    // uso de HU09 utiliza temporal=false.
    Task CambiarContrasenaAsync(
        string idKeycloak,
        string nuevaContrasena,
        bool temporal,
        CancellationToken cancelacion);
}

// Datos mínimos que necesita el proveedor de identidad (Keycloak) para crear
// la cuenta. La contraseña viaja solo a Keycloak (nunca a PostgreSQL).
public sealed record DatosCreacionUsuarioIdentidad(
    string NombreUsuario,
    string Correo,
    string Contrasena,
    string Nombre,
    string Apellido);

// HU09 — payload de actualización parcial hacia Keycloak. Las propiedades en
// null significan "no enviar este campo" — la implementación debe omitirlos
// del cuerpo JSON para no sobrescribir valores existentes.
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
