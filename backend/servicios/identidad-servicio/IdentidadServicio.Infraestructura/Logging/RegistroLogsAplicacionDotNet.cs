using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using IdentidadServicio.Aplicacion.Puertos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Infraestructura.Logging;

public sealed class RegistroLogsAplicacionDotNet : IRegistroLogsAplicacion
{
    private const string Anonimo = "Anonimo";
    private const string NoDisponible = "NoDisponible";
    private const string SinPropiedades = "{}";
    private const string ValorRedactado = "***REDACTADO***";
    private const string HeaderCorrelacion = "X-Correlation-Id";
    private const string ItemCorrelacion = "CorrelationId";

    private static readonly string[] FragmentosSensibles =
    {
        "contrasena", "contraseña", "password", "token",
        "refresh", "authorization", "secret", "hash"
    };

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private const string Plantilla =
        "EventoAplicacion={EventoAplicacion}, Descripcion={Descripcion}, Propiedades={Propiedades}, " +
        "CorrelationId={CorrelationId}, ActorId={ActorId}, ActorUsuario={ActorUsuario}, ActorRol={ActorRol}";

    private readonly ILogger<RegistroLogsAplicacionDotNet> _logger;
    private readonly IHttpContextAccessor _accesorHttp;

    public RegistroLogsAplicacionDotNet(
        ILogger<RegistroLogsAplicacionDotNet> logger,
        IHttpContextAccessor accesorHttp)
    {
        _logger = logger;
        _accesorHttp = accesorHttp;
    }

    public void Informacion(
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades = null)
        => Registrar(LogLevel.Information, exception: null, evento, descripcion, propiedades);

    public void Advertencia(
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades = null)
        => Registrar(LogLevel.Warning, exception: null, evento, descripcion, propiedades);

    public void Error(
        Exception excepcion,
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades = null)
        => Registrar(LogLevel.Error, excepcion, evento, descripcion, propiedades);

    private void Registrar(
        LogLevel nivel,
        Exception? exception,
        string evento,
        string descripcion,
        IReadOnlyDictionary<string, object?>? propiedades)
    {
        var (correlationId, actorId, actorUsuario, actorRol) = ObtenerContextoActor();
        var propiedadesRedactadas = RedactarPropiedades(propiedades);
        using var scope = CrearScope(propiedadesRedactadas);
        _logger.Log(
            nivel,
            exception,
            Plantilla,
            evento,
            descripcion,
            SerializarPropiedades(propiedadesRedactadas),
            correlationId,
            actorId,
            actorUsuario,
            actorRol);
    }

    private IDisposable? CrearScope(IReadOnlyDictionary<string, object?>? propiedades)
    {
        return propiedades is null || propiedades.Count == 0
            ? null
            : _logger.BeginScope(propiedades);
    }

    // Redacta las claves sensibles una sola vez. El resultado se usa tanto para
    // el mensaje como para el scope (IncludeScopes = true en consola/Docker),
    // de modo que ningún valor sensible se filtre por el scope. No muta el
    // diccionario original.
    private static IReadOnlyDictionary<string, object?>? RedactarPropiedades(
        IReadOnlyDictionary<string, object?>? propiedades)
    {
        if (propiedades is null || propiedades.Count == 0)
            return null;

        var redactadas = new Dictionary<string, object?>(propiedades.Count);
        foreach (var (clave, valor) in propiedades)
            redactadas[clave] = EsClaveSensible(clave) ? ValorRedactado : valor;

        return redactadas;
    }

    private static string SerializarPropiedades(IReadOnlyDictionary<string, object?>? propiedades)
    {
        if (propiedades is null || propiedades.Count == 0)
            return SinPropiedades;

        try
        {
            return JsonSerializer.Serialize(propiedades, OpcionesJson);
        }
        catch
        {
            // Nunca dejar que un valor no serializable rompa el logging.
            return "{\"error\":\"propiedades-no-serializables\"}";
        }
    }

    private static bool EsClaveSensible(string clave) =>
        FragmentosSensibles.Any(fragmento =>
            clave.Contains(fragmento, StringComparison.OrdinalIgnoreCase));

    private (string CorrelationId, string ActorId, string ActorUsuario, string ActorRol)
        ObtenerContextoActor()
    {
        var contexto = _accesorHttp.HttpContext;
        if (contexto is null)
            return (NoDisponible, Anonimo, Anonimo, Anonimo);

        var correlationId = ObtenerCorrelationId(contexto);
        var usuario = contexto.User;

        if (usuario.Identity?.IsAuthenticated != true)
            return (correlationId, Anonimo, Anonimo, Anonimo);

        return (
            correlationId,
            ObtenerPrimerClaim(usuario, ClaimTypes.NameIdentifier, "sub", "nameid") ?? NoDisponible,
            ObtenerPrimerClaim(usuario, "preferred_username", "name", "unique_name")
                ?? usuario.Identity?.Name ?? NoDisponible,
            ObtenerRol(usuario));
    }

    private static string ObtenerCorrelationId(HttpContext contexto)
    {
        if (contexto.Items.TryGetValue(ItemCorrelacion, out var valor)
            && valor is string id && !string.IsNullOrWhiteSpace(id))
            return id;

        var header = contexto.Request.Headers[HeaderCorrelacion].ToString();
        return string.IsNullOrWhiteSpace(header) ? NoDisponible : header;
    }

    private static string? ObtenerPrimerClaim(ClaimsPrincipal usuario, params string[] tipos) =>
        tipos
            .Select(tipo => usuario.FindFirst(tipo)?.Value)
            .FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor));

    private static string ObtenerRol(ClaimsPrincipal usuario)
    {
        var roles = new List<string>();

        AgregarRoles(roles, usuario.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
        AgregarRoles(roles, usuario.FindAll("role").Select(claim => claim.Value));
        AgregarRoles(roles, usuario.FindAll("roles").Select(claim => claim.Value));
        AgregarRoles(
            roles,
            usuario.Claims
                .Where(claim => claim.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value));

        foreach (var claim in usuario.FindAll("realm_access"))
            AgregarRolesRealmAccess(roles, claim.Value);

        return roles.Count == 0 ? NoDisponible : string.Join(",", roles);
    }

    private static void AgregarRoles(List<string> roles, IEnumerable<string> valores)
    {
        foreach (var valor in valores)
        {
            foreach (var candidato in valor.Split(
                         [',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var rol = NormalizarRol(candidato.Trim().Trim('"', '[', ']'));
                if (rol is not null && !roles.Contains(rol, StringComparer.Ordinal))
                    roles.Add(rol);
            }
        }
    }

    private static void AgregarRolesRealmAccess(List<string> roles, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return;

        try
        {
            using var documento = JsonDocument.Parse(valor);
            if (documento.RootElement.ValueKind != JsonValueKind.Object
                || !documento.RootElement.TryGetProperty("roles", out var elementoRoles)
                || elementoRoles.ValueKind != JsonValueKind.Array)
                return;

            AgregarRoles(
                roles,
                elementoRoles.EnumerateArray()
                    .Where(elemento => elemento.ValueKind == JsonValueKind.String)
                    .Select(elemento => elemento.GetString()!));
        }
        catch (JsonException)
        {
            // Un claim mal formado no debe interrumpir el registro técnico.
        }
    }

    private static string? NormalizarRol(string valor)
    {
        if (valor.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            return "Administrador";
        if (valor.Equals("Operador", StringComparison.OrdinalIgnoreCase))
            return "Operador";
        if (valor.Equals("Participante", StringComparison.OrdinalIgnoreCase))
            return "Participante";
        return null;
    }
}
