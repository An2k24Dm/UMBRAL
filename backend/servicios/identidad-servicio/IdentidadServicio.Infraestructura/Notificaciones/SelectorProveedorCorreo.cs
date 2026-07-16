using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Infraestructura.Notificaciones;

/// <summary>
/// Resuelve la implementación de <see cref="IServicioCorreo"/> a partir del valor
/// configurado en <c>EnvioCorreo:Proveedor</c>. Es una función pura (sin estado ni
/// dependencias del contenedor) para poder probar la selección de forma aislada.
/// </summary>
public static class SelectorProveedorCorreo
{
    public const string Smtp = "Smtp";
    public const string GmailApi = "GmailApi";

    /// <summary>
    /// Devuelve el servicio de correo según <paramref name="proveedor"/>:
    /// vacío o ausente => SMTP; <c>Smtp</c> => SMTP; <c>GmailApi</c> => Gmail API
    /// (comparación insensible a mayúsculas). Cualquier otro valor lanza una
    /// excepción clara. Las fábricas se invocan de forma perezosa para construir
    /// únicamente la implementación seleccionada.
    /// </summary>
    public static IServicioCorreo Resolver(
        string? proveedor,
        Func<IServicioCorreo> crearSmtp,
        Func<IServicioCorreo> crearGmailApi)
    {
        ArgumentNullException.ThrowIfNull(crearSmtp);
        ArgumentNullException.ThrowIfNull(crearGmailApi);

        var valor = proveedor?.Trim();

        if (string.IsNullOrEmpty(valor) ||
            valor.Equals(Smtp, StringComparison.OrdinalIgnoreCase))
            return crearSmtp();

        if (valor.Equals(GmailApi, StringComparison.OrdinalIgnoreCase))
            return crearGmailApi();

        throw new InvalidOperationException(
            $"Proveedor de correo no soportado: '{proveedor}'. " +
            $"Valores válidos para EnvioCorreo:Proveedor: '{Smtp}', '{GmailApi}'.");
    }
}
