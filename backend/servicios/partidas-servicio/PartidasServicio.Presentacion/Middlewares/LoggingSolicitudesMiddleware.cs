using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace PartidasServicio.Presentacion.Middlewares;

public sealed class LoggingSolicitudesMiddleware
{
    public const string HeaderCorrelacion = "X-Correlation-Id";
    public const string ItemCorrelacion = "CorrelationId";
    private const string Servicio = "partidas-servicio";
    private const string Anonimo = "Anonimo";
    private const string NoDisponible = "NoDisponible";

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<LoggingSolicitudesMiddleware> _registro;

    public LoggingSolicitudesMiddleware(
        RequestDelegate siguiente, ILogger<LoggingSolicitudesMiddleware> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    public async Task Invoke(HttpContext contexto)
    {
        var correlationId = ObtenerOCrearCorrelationId(contexto.Request);
        contexto.Items[ItemCorrelacion] = correlationId;
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers[HeaderCorrelacion] = correlationId;
            return Task.CompletedTask;
        });

        var cronometro = Stopwatch.StartNew();
        using var _ = _registro.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Servicio"] = Servicio
        });

        try
        {
            await _siguiente(contexto);
        }
        finally
        {
            cronometro.Stop();
            _registro.LogInformation(
                "Servicio={Servicio}, HTTP {Metodo} {Ruta} respondió {CodigoEstado} en {DuracionMs}ms. " +
                "UsuarioId={UsuarioId}, Usuario={Usuario}, Rol={Rol}, CorrelationId={CorrelationId}",
                Servicio,
                contexto.Request.Method,
                contexto.Request.Path.Value,
                contexto.Response.StatusCode,
                cronometro.ElapsedMilliseconds,
                ObtenerUsuarioId(contexto.User),
                ObtenerNombreUsuario(contexto.User),
                ObtenerRol(contexto.User),
                correlationId);
        }
    }

    private static string ObtenerOCrearCorrelationId(HttpRequest solicitud)
    {
        var recibido = solicitud.Headers[HeaderCorrelacion].ToString();
        return string.IsNullOrWhiteSpace(recibido) ? Guid.NewGuid().ToString() : recibido;
    }

    private static bool EstaAutenticado(ClaimsPrincipal usuario) =>
        usuario.Identity?.IsAuthenticated == true;

    private static string ObtenerUsuarioId(ClaimsPrincipal usuario)
    {
        if (!EstaAutenticado(usuario)) return Anonimo;
        return ObtenerPrimerClaim(usuario, ClaimTypes.NameIdentifier, "sub") ?? NoDisponible;
    }

    private static string ObtenerNombreUsuario(ClaimsPrincipal usuario)
    {
        if (!EstaAutenticado(usuario)) return Anonimo;
        return ObtenerPrimerClaim(usuario, "preferred_username", "name")
               ?? usuario.Identity?.Name ?? NoDisponible;
    }

    private static string ObtenerRol(ClaimsPrincipal usuario)
    {
        if (!EstaAutenticado(usuario)) return Anonimo;
        var roles = new List<string>();
        AgregarRoles(roles, usuario.FindAll("roles").Select(c => c.Value));
        foreach (var claim in usuario.FindAll("realm_access"))
            AgregarRolesRealmAccess(roles, claim.Value);
        return roles.Count == 0 ? NoDisponible : string.Join(",", roles);
    }

    private static string? ObtenerPrimerClaim(ClaimsPrincipal usuario, params string[] tipos) =>
        tipos.Select(t => usuario.FindFirst(t)?.Value)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static void AgregarRoles(List<string> roles, IEnumerable<string> valores)
    {
        foreach (var valor in valores)
            foreach (var candidato in valor.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var rol = NormalizarRol(candidato.Trim().Trim('"', '[', ']'));
                if (rol is not null && !roles.Contains(rol, StringComparer.Ordinal))
                    roles.Add(rol);
            }
    }

    private static void AgregarRolesRealmAccess(List<string> roles, string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        try
        {
            using var doc = JsonDocument.Parse(valor);
            if (!doc.RootElement.TryGetProperty("roles", out var elementoRoles)
                || elementoRoles.ValueKind != JsonValueKind.Array) return;
            AgregarRoles(roles, elementoRoles.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!));
        }
        catch (JsonException) { }
    }

    private static string? NormalizarRol(string valor)
    {
        if (valor.Equals("Administrador", StringComparison.OrdinalIgnoreCase)) return "Administrador";
        if (valor.Equals("Operador", StringComparison.OrdinalIgnoreCase)) return "Operador";
        if (valor.Equals("Participante", StringComparison.OrdinalIgnoreCase)) return "Participante";
        return null;
    }
}
