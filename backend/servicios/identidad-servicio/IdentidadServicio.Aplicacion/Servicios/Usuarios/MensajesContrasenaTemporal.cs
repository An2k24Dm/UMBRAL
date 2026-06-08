namespace IdentidadServicio.Aplicacion.Servicios.Usuarios;

public static class MensajesContrasenaTemporal
{
    public const string AsuntoCreacion = "Cuenta creada en UMBRAL";
    public const string AsuntoReseteo = "Contraseña temporal de UMBRAL";

    public static string CuerpoCreacion(
        string nombreCompleto,
        string nombreUsuario,
        string correoAcceso,
        string contrasenaTemporal,
        string rol) =>
$@"Hola {nombreCompleto},

Tu cuenta en UMBRAL fue creada con rol {rol}.

Datos de acceso:
  Usuario: {nombreUsuario}
  Correo:  {correoAcceso}
  Contraseña temporal: {contrasenaTemporal}

Esta contraseña es de un solo uso. Al iniciar sesión por primera vez en el
panel web te pediremos cambiarla por una nueva contraseña que solo tú
conozcas.

Si no esperabas este correo, contacta a un administrador de UMBRAL.

— Equipo UMBRAL";

    public static string CuerpoReseteo(
        string nombreCompleto,
        string nombreUsuario,
        string correoAcceso,
        string contrasenaTemporal) =>
$@"Hola {nombreCompleto},

Un administrador de UMBRAL solicitó el reseteo de tu contraseña.

Datos de acceso:
  Usuario: {nombreUsuario}
  Correo:  {correoAcceso}
  Contraseña temporal: {contrasenaTemporal}

Esta contraseña es de un solo uso. Al iniciar sesión te pediremos cambiarla
por una nueva contraseña que solo tú conozcas.

Si no solicitaste este reseteo, contacta a un administrador de UMBRAL.

— Equipo UMBRAL";
}
