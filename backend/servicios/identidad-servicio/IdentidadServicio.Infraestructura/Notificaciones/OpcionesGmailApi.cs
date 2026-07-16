namespace IdentidadServicio.Infraestructura.Notificaciones;

/// <summary>
/// Credenciales y remitente para el envío por Gmail API (sección <c>GmailApi</c>).
/// Se pueblan por variables de entorno en el despliegue (Render); nunca se versionan
/// valores reales. La selección del proveedor sigue siendo <c>EnvioCorreo:Proveedor</c>.
/// </summary>
public sealed class OpcionesGmailApi
{
    public const string Seccion = "GmailApi";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string RemitenteCorreo { get; set; } = string.Empty;
    public string RemitenteNombre { get; set; } = "UMBRAL";
}
