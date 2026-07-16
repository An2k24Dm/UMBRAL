namespace IdentidadServicio.Infraestructura.Notificaciones;

/// <summary>
/// Selección del proveedor de correo por configuración (sección <c>EnvioCorreo</c>).
/// La resolución concreta de <see cref="IdentidadServicio.Aplicacion.Puertos.IServicioCorreo"/>
/// se hace en la composición de dependencias a partir de <see cref="Proveedor"/>.
/// Valor predeterminado: <c>Smtp</c> (comportamiento actual para desarrollo local).
/// </summary>
public sealed class OpcionesEnvioCorreo
{
    public const string Seccion = "EnvioCorreo";

    /// <summary>Valores válidos: <c>Smtp</c> o <c>GmailApi</c>. Vacío o ausente = <c>Smtp</c>.</summary>
    public string Proveedor { get; set; } = "Smtp";
}
