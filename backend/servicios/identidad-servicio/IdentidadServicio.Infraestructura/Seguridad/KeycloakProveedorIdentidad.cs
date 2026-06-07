using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using IdentidadServicio.Aplicacion.Puertos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentidadServicio.Infraestructura.Seguridad;

public sealed class OpcionesKeycloak
{
    public const string Seccion = "Keycloak";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AdminUsuario { get; set; } = "admin";
    public string AdminContrasena { get; set; } = "admin";

    public string UrlBase
    {
        get
        {
            if (string.IsNullOrEmpty(Authority)) return string.Empty;
            var uri = new Uri(Authority);
            return $"{uri.Scheme}://{uri.Authority}";
        }
    }

    public string Realm
    {
        get
        {
            if (string.IsNullOrEmpty(Authority)) return string.Empty;
            return Authority.TrimEnd('/').Split('/').Last();
        }
    }

    public string UrlToken => $"{Authority.TrimEnd('/')}/protocol/openid-connect/token";
    public string UrlJwks => $"{Authority.TrimEnd('/')}/protocol/openid-connect/certs";
    public string MetadataAddress => $"{Authority.TrimEnd('/')}/.well-known/openid-configuration";
    public string UrlTokenAdmin => $"{UrlBase}/realms/master/protocol/openid-connect/token";
    public string UrlAdminUsuarios => $"{UrlBase}/admin/realms/{Realm}/users";
    public string UrlAdminUsuario(string id) => $"{UrlBase}/admin/realms/{Realm}/users/{id}";
    public string UrlAdminRol(string nombre) => $"{UrlBase}/admin/realms/{Realm}/roles/{nombre}";
    public string UrlAdminAsignarRol(string id) => $"{UrlBase}/admin/realms/{Realm}/users/{id}/role-mappings/realm";
    public string UrlAdminCambiarContrasena(string id) => $"{UrlBase}/admin/realms/{Realm}/users/{id}/reset-password";
}

public sealed class KeycloakProveedorIdentidad : IProveedorIdentidad
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _cliente;
    private readonly OpcionesKeycloak _opciones;
    private readonly ILogger<KeycloakProveedorIdentidad> _registro;

    public KeycloakProveedorIdentidad(
        HttpClient cliente,
        IOptions<OpcionesKeycloak> opciones,
        ILogger<KeycloakProveedorIdentidad> registro)
    {
        _cliente = cliente;
        _opciones = opciones.Value;
        _registro = registro;
    }

    public async Task<ResultadoAutenticacionExterna?> IniciarSesionAsync(
        string nombreUsuario, string contrasena, CancellationToken cancelacion)
    {
        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _opciones.ClientId,
            ["client_secret"] = _opciones.ClientSecret,
            ["username"] = nombreUsuario,
            ["password"] = contrasena,
            ["scope"] = "openid"
        });

        using var respuesta = await _cliente.PostAsync(_opciones.UrlToken, contenido, cancelacion);
        if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            _registro.LogWarning(
                "Keycloak rechazó credenciales de {Nombre}. Estado={Estado}.",
                nombreUsuario, (int)respuesta.StatusCode);
            return null;
        }
        respuesta.EnsureSuccessStatusCode();

        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);
        var token = JsonSerializer.Deserialize<RespuestaToken>(cuerpo)
                    ?? throw new InvalidOperationException("Respuesta de Keycloak vacía.");

        var idKeycloak = LeerSubDelToken(token.AccessToken);

        return new ResultadoAutenticacionExterna(
            token.AccessToken, token.RefreshToken ?? string.Empty,
            token.ExpiresIn, token.TokenType ?? "Bearer", idKeycloak);
    }

    public async Task<string> CrearUsuarioAsync(
        DatosCreacionUsuarioIdentidad datos, CancellationToken cancelacion)
    {
        var tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

        var cuerpoUsuario = new
        {
            username = datos.NombreUsuario,
            email = datos.Correo,
            firstName = datos.Nombre,
            lastName = datos.Apellido,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = "password", value = datos.Contrasena, temporary = false }
            }
        };

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, _opciones.UrlAdminUsuarios)
        {
            Content = JsonContent.Create(cuerpoUsuario, options: OpcionesJson)
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(cancelacion);
            _registro.LogError("Keycloak rechazó creación de {Nombre}. {Estado} {Cuerpo}",
                datos.NombreUsuario, respuesta.StatusCode, detalle);
            respuesta.EnsureSuccessStatusCode();
        }

        var ubicacion = respuesta.Headers.Location?.ToString()
                        ?? throw new InvalidOperationException("Keycloak no devolvió Location.");
        return ubicacion.TrimEnd('/').Split('/').Last();
    }

    public async Task AsignarRolAsync(string idKeycloak, string nombreRol, CancellationToken cancelacion)
    {
        var tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

        using var solRol = new HttpRequestMessage(HttpMethod.Get, _opciones.UrlAdminRol(nombreRol));
        solRol.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        using var respRol = await _cliente.SendAsync(solRol, cancelacion);
        respRol.EnsureSuccessStatusCode();
        var rolJson = await respRol.Content.ReadAsStringAsync(cancelacion);

        using var solAsig = new HttpRequestMessage(HttpMethod.Post, _opciones.UrlAdminAsignarRol(idKeycloak))
        {
            Content = new StringContent($"[{rolJson}]", Encoding.UTF8, "application/json")
        };
        solAsig.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        using var respAsig = await _cliente.SendAsync(solAsig, cancelacion);
        respAsig.EnsureSuccessStatusCode();
    }

    public async Task ActualizarUsuarioAsync(
        string idKeycloak,
        DatosActualizacionUsuarioIdentidad datos,
        CancellationToken cancelacion)
    {
        if (!datos.TieneCambios) return;

        var tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);

        // 1) Traer el UserRepresentation actual.
        using var solGet = new HttpRequestMessage(HttpMethod.Get, _opciones.UrlAdminUsuario(idKeycloak));
        solGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        using var respGet = await _cliente.SendAsync(solGet, cancelacion);
        if (!respGet.IsSuccessStatusCode)
        {
            var detalle = await respGet.Content.ReadAsStringAsync(cancelacion);
            _registro.LogError(
                "Keycloak rechazó la lectura del usuario {Id}. {Estado} {Cuerpo}",
                idKeycloak, respGet.StatusCode, detalle);
            respGet.EnsureSuccessStatusCode();
        }

        var json = await respGet.Content.ReadAsStringAsync(cancelacion);
        if (JsonNode.Parse(json) is not JsonObject representacion)
        {
            _registro.LogError(
                "Keycloak devolvió un cuerpo inesperado para el usuario {Id}: {Cuerpo}",
                idKeycloak, json);
            throw new InvalidOperationException(
                $"Keycloak devolvió un cuerpo inesperado para el usuario {idKeycloak}.");
        }

        // 2) Mutar solo los campos que el caso de uso pidió cambiar.
        if (datos.NombreUsuario is not null) representacion["username"] = datos.NombreUsuario;
        if (datos.Correo is not null) representacion["email"] = datos.Correo;
        if (datos.Nombre is not null) representacion["firstName"] = datos.Nombre;
        if (datos.Apellido is not null) representacion["lastName"] = datos.Apellido;

        // 3) Enviar el representation completo de vuelta.
        using var solPut = new HttpRequestMessage(HttpMethod.Put, _opciones.UrlAdminUsuario(idKeycloak))
        {
            Content = new StringContent(representacion.ToJsonString(), Encoding.UTF8, "application/json")
        };
        solPut.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        using var respPut = await _cliente.SendAsync(solPut, cancelacion);
        if (!respPut.IsSuccessStatusCode)
        {
            var detalle = await respPut.Content.ReadAsStringAsync(cancelacion);
            _registro.LogError(
                "Keycloak rechazó la actualización del usuario {Id}. {Estado} {Cuerpo}",
                idKeycloak, respPut.StatusCode, detalle);
            respPut.EnsureSuccessStatusCode();
        }
    }

    public async Task CambiarContrasenaAsync(
        string idKeycloak,
        string nuevaContrasena,
        CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(idKeycloak))
            throw new ArgumentException(
                "IdKeycloak es obligatorio.", nameof(idKeycloak));
        if (string.IsNullOrEmpty(nuevaContrasena))
            throw new ArgumentException(
                "La nueva contraseña es obligatoria.", nameof(nuevaContrasena));

        var tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);
        
        var cuerpoCredencial = new
        {
            type = "password",
            value = nuevaContrasena,
            temporary = false
        };

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Put, _opciones.UrlAdminCambiarContrasena(idKeycloak))
        {
            Content = JsonContent.Create(cuerpoCredencial, options: OpcionesJson)
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);

        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(cancelacion);
            // Nunca se loguea la contraseña: solo id, status y cuerpo del error.
            _registro.LogError(
                "Keycloak rechazó el cambio de contraseña del usuario {IdKeycloak}. " +
                "Estado={Estado} Cuerpo={Cuerpo}",
                idKeycloak, (int)respuesta.StatusCode, detalle);
            respuesta.EnsureSuccessStatusCode();
        }

        _registro.LogInformation(
            "Keycloak aceptó el cambio de contraseña del usuario {IdKeycloak} " +
            "(estado={Estado}).",
            idKeycloak, (int)respuesta.StatusCode);
    }

    public async Task EliminarUsuarioAsync(string idKeycloak, CancellationToken cancelacion)
    {
        var tokenAdmin = await ObtenerTokenAdminAsync(cancelacion);
        using var solicitud = new HttpRequestMessage(HttpMethod.Delete, _opciones.UrlAdminUsuario(idKeycloak));
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAdmin);
        using var respuesta = await _cliente.SendAsync(solicitud, cancelacion);
        if (respuesta.StatusCode == HttpStatusCode.NotFound) return;
        respuesta.EnsureSuccessStatusCode();
    }

    private async Task<string> ObtenerTokenAdminAsync(CancellationToken cancelacion)
    {
        var contenido = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = _opciones.AdminUsuario,
            ["password"] = _opciones.AdminContrasena
        });

        using var respuesta = await _cliente.PostAsync(_opciones.UrlTokenAdmin, contenido, cancelacion);
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);
        using var doc = JsonDocument.Parse(cuerpo);
        return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
    }

    private static string LeerSubDelToken(string jwt)
    {
        var partes = jwt.Split('.');
        if (partes.Length < 2) return string.Empty;
        var payload = Base64UrlABytes(partes[1]);
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.TryGetProperty("sub", out var sub) ? (sub.GetString() ?? string.Empty) : string.Empty;
    }

    private static byte[] Base64UrlABytes(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
    }

    private sealed class RespuestaToken
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}
