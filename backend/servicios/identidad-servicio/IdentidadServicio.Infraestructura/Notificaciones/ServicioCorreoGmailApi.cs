using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentidadServicio.Aplicacion.Puertos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentidadServicio.Infraestructura.Notificaciones;

/// <summary>
/// Implementación de <see cref="IServicioCorreo"/> que envía correos mediante la
/// Gmail API por HTTPS usando OAuth 2.0 (refresh token). No usa SMTP.
/// Alterna con <c>ServicioCorreoSmtp</c> según <c>EnvioCorreo:Proveedor</c>.
/// </summary>
public sealed class ServicioCorreoGmailApi : IServicioCorreo
{
    private const string UrlToken = "https://oauth2.googleapis.com/token";
    private const string UrlEnvio =
        "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly OpcionesGmailApi _opciones;
    private readonly ILogger<ServicioCorreoGmailApi> _registro;

    public ServicioCorreoGmailApi(
        HttpClient http,
        IOptions<OpcionesGmailApi> opciones,
        ILogger<ServicioCorreoGmailApi> registro)
    {
        _http = http;
        _opciones = opciones.Value;
        _registro = registro;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoTextoPlano,
        CancellationToken cancelacion)
    {
        ValidarConfiguracion(destinatario);

        var cronometro = Stopwatch.StartNew();
        try
        {
            var accessToken = await ObtenerAccessTokenAsync(cancelacion);

            var mime = ConstruirMime(
                _opciones.RemitenteNombre,
                _opciones.RemitenteCorreo,
                destinatario,
                asunto,
                cuerpoTextoPlano);

            var raw = Base64UrlSafe(mime);

            var messageId = await EnviarMensajeAsync(accessToken, raw, cancelacion);

            cronometro.Stop();
            _registro.LogInformation(
                "Correo enviado vía {Proveedor} a {Destino}. MessageId={MessageId}. " +
                "DuracionMs={DuracionMs}.",
                "GmailApi", destinatario, messageId, cronometro.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            // Cancelación provocada por la finalización de la solicitud HTTP entrante.
            // Se re-propaga tal cual: NO es un fallo de credenciales ni de Gmail.
            _registro.LogWarning(
                "Envío de correo (GmailApi) cancelado porque la solicitud HTTP terminó. " +
                "Destino={Destino}.", destinatario);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            // El token de cancelación del llamante NO fue disparado: es el timeout
            // propio de HttpClient al contactar Gmail API.
            _registro.LogError(ex,
                "Timeout propio contactando Gmail API (proveedor=GmailApi). " +
                "Destino={Destino}.", destinatario);
            throw new ExcepcionEnvioCorreoGmail(
                "Se agotó el tiempo de espera al contactar la Gmail API.", ex);
        }
    }

    private void ValidarConfiguracion(string destinatario)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("Destinatario obligatorio.", nameof(destinatario));

        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(_opciones.ClientId)) faltantes.Add(nameof(OpcionesGmailApi.ClientId));
        if (string.IsNullOrWhiteSpace(_opciones.ClientSecret)) faltantes.Add(nameof(OpcionesGmailApi.ClientSecret));
        if (string.IsNullOrWhiteSpace(_opciones.RefreshToken)) faltantes.Add(nameof(OpcionesGmailApi.RefreshToken));
        if (string.IsNullOrWhiteSpace(_opciones.RemitenteCorreo)) faltantes.Add(nameof(OpcionesGmailApi.RemitenteCorreo));

        if (faltantes.Count > 0)
        {
            // Solo se nombran los campos faltantes; nunca se imprimen sus valores.
            throw new ExcepcionEnvioCorreoGmail(
                "Configuración de Gmail API incompleta. Faltan valores en la sección " +
                $"'GmailApi': {string.Join(", ", faltantes)}.");
        }
    }

    private async Task<string> ObtenerAccessTokenAsync(CancellationToken cancelacion)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, UrlToken)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _opciones.ClientId,
                ["client_secret"] = _opciones.ClientSecret,
                ["refresh_token"] = _opciones.RefreshToken,
                ["grant_type"] = "refresh_token"
            })
        };

        using var respuesta = await _http.SendAsync(solicitud, cancelacion);
        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = ExtraerDetalleErrorOAuth(cuerpo);
            _registro.LogError(
                "Fallo al obtener access token de Google OAuth (proveedor=GmailApi). " +
                "HttpStatus={Status}. Detalle={Detalle}.",
                (int)respuesta.StatusCode, detalle);
            throw new ExcepcionEnvioCorreoGmail(
                $"Google rechazó la solicitud de token (HTTP {(int)respuesta.StatusCode}).");
        }

        RespuestaToken? token;
        try
        {
            token = JsonSerializer.Deserialize<RespuestaToken>(cuerpo, OpcionesJson);
        }
        catch (JsonException ex)
        {
            throw new ExcepcionEnvioCorreoGmail(
                "Respuesta de token de Google con formato inesperado.", ex);
        }

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            throw new ExcepcionEnvioCorreoGmail(
                "La respuesta de token de Google no incluyó un access_token.");

        return token.AccessToken;
    }

    private async Task<string?> EnviarMensajeAsync(
        string accessToken, string raw, CancellationToken cancelacion)
    {
        var json = JsonSerializer.Serialize(new SolicitudEnvio(raw));

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, UrlEnvio)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var respuesta = await _http.SendAsync(solicitud, cancelacion);
        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            _registro.LogError(
                "Fallo al enviar el correo por Gmail API (proveedor=GmailApi). " +
                "HttpStatus={Status}.", (int)respuesta.StatusCode);
            throw new ExcepcionEnvioCorreoGmail(
                $"Gmail API rechazó el envío (HTTP {(int)respuesta.StatusCode}).");
        }

        try
        {
            var enviado = JsonSerializer.Deserialize<RespuestaEnvio>(cuerpo, OpcionesJson);
            return enviado?.Id;
        }
        catch (JsonException)
        {
            // El envío fue aceptado (2xx); si no podemos leer el id no es un fallo de envío.
            return null;
        }
    }

    /// <summary>
    /// Construye un mensaje MIME de texto plano UTF-8. El asunto se codifica según
    /// RFC 2047 (<c>=?UTF-8?B?...?=</c>) para transportar acentos/ñ de forma segura
    /// en la cabecera; el cuerpo va como UTF-8 con saltos de línea CRLF.
    /// </summary>
    internal static string ConstruirMime(
        string remitenteNombre,
        string remitenteCorreo,
        string destinatario,
        string asunto,
        string cuerpoTextoPlano)
    {
        var from = string.IsNullOrWhiteSpace(remitenteNombre)
            ? remitenteCorreo
            : $"{CodificarCabeceraRfc2047(remitenteNombre)} <{remitenteCorreo}>";

        var cuerpoNormalizado = cuerpoTextoPlano.Replace("\r\n", "\n").Replace("\n", "\r\n");

        var sb = new StringBuilder();
        sb.Append("From: ").Append(from).Append("\r\n");
        sb.Append("To: ").Append(destinatario).Append("\r\n");
        sb.Append("Subject: ").Append(CodificarCabeceraRfc2047(asunto)).Append("\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: text/plain; charset=UTF-8\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit\r\n");
        sb.Append("\r\n");
        sb.Append(cuerpoNormalizado);
        return sb.ToString();
    }

    /// <summary>Codifica una cabecera como encoded-word RFC 2047 si contiene no-ASCII.</summary>
    internal static string CodificarCabeceraRfc2047(string valor)
    {
        if (EsAscii(valor))
            return valor;

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(valor));
        return $"=?UTF-8?B?{base64}?=";
    }

    private static bool EsAscii(string valor)
    {
        foreach (var c in valor)
            if (c > 127) return false;
        return true;
    }

    internal static string Base64UrlSafe(string mime)
    {
        var bytes = Encoding.UTF8.GetBytes(mime);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string ExtraerDetalleErrorOAuth(string cuerpo)
    {
        // Devuelve solo los códigos de error de OAuth (p. ej. "invalid_grant"),
        // nunca tokens ni el cuerpo completo. Se acota la longitud por seguridad.
        try
        {
            var error = JsonSerializer.Deserialize<RespuestaErrorOAuth>(cuerpo, OpcionesJson);
            if (error is not null && !string.IsNullOrWhiteSpace(error.Error))
            {
                var desc = string.IsNullOrWhiteSpace(error.ErrorDescription)
                    ? string.Empty
                    : $" ({Acotar(error.ErrorDescription, 120)})";
                return $"{error.Error}{desc}";
            }
        }
        catch (JsonException)
        {
            // Cuerpo no-JSON: se ignora para no arriesgar volcar contenido sensible.
        }
        return "(sin detalle estructurado)";
    }

    private static string Acotar(string valor, int max) =>
        valor.Length <= max ? valor : valor[..max] + "…";

    private sealed record RespuestaToken(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("token_type")] string? TokenType);

    private sealed record RespuestaEnvio(
        [property: JsonPropertyName("id")] string? Id);

    private sealed record RespuestaErrorOAuth(
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);

    private sealed record SolicitudEnvio(
        [property: JsonPropertyName("raw")] string Raw);
}

/// <summary>
/// Error controlado al enviar correo por Gmail API (configuración incompleta,
/// fallo de token, rechazo de Gmail o timeout propio). No transporta secretos.
/// </summary>
public sealed class ExcepcionEnvioCorreoGmail : Exception
{
    public ExcepcionEnvioCorreoGmail(string mensaje) : base(mensaje) { }
    public ExcepcionEnvioCorreoGmail(string mensaje, Exception interna)
        : base(mensaje, interna) { }
}
