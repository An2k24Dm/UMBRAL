using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace JuegosServicio.Presentacion.Middlewares;

public sealed class LoggingSolicitudesMiddleware
{
    public const string HeaderCorrelacion = "X-Correlation-Id";
    public const string ItemCorrelacion = "CorrelationId";
    private const string Servicio = "juegos-servicio";
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
        contexto.Response.Headers[HeaderCorrelacion] = correlationId;
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
                "Servicio={Servicio}, Descripcion={Descripcion}, HTTP {Metodo} {Ruta} respondió {CodigoEstado} en {DuracionMs}ms. " +
                "UsuarioId={UsuarioId}, Usuario={Usuario}, Rol={Rol}, Ip={Ip}, UserAgent={UserAgent}, " +
                "CorrelationId={CorrelationId}",
                Servicio,
                ObtenerDescripcion(contexto),
                contexto.Request.Method,
                contexto.Request.Path.Value,
                contexto.Response.StatusCode,
                cronometro.ElapsedMilliseconds,
                ObtenerUsuarioId(contexto.User),
                ObtenerNombreUsuario(contexto.User),
                ObtenerRol(contexto.User),
                contexto.Connection.RemoteIpAddress?.ToString(),
                contexto.Request.Headers.UserAgent.ToString(),
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
        if (!EstaAutenticado(usuario))
            return Anonimo;

        return ObtenerPrimerClaim(usuario, ClaimTypes.NameIdentifier, "sub", "nameid")
               ?? NoDisponible;
    }

    private static string ObtenerNombreUsuario(ClaimsPrincipal usuario)
    {
        if (!EstaAutenticado(usuario))
            return Anonimo;

        return ObtenerPrimerClaim(usuario, "preferred_username", "name", "unique_name")
               ?? usuario.Identity?.Name
               ?? NoDisponible;
    }

    private static string ObtenerRol(ClaimsPrincipal usuario)
    {
        if (!EstaAutenticado(usuario))
            return Anonimo;

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

    private static string ObtenerDescripcion(HttpContext contexto)
    {
        var codigo = contexto.Response.StatusCode;
        var metodo = contexto.Request.Method;
        var ruta = contexto.Request.Path.Value?.TrimEnd('/').ToLowerInvariant() ?? string.Empty;

        var descripcionError = ObtenerDescripcionError(codigo);
        if (descripcionError is not null)
            return descripcionError;

        if (codigo is >= 200 and < 300)
        {
            // Sub-recursos: preguntas (trivia), pistas (búsqueda), etapas (misión).
            if (ruta.Contains("/preguntas", StringComparison.Ordinal))
            {
                if (HttpMethods.IsPost(metodo)) return "Usuario agregó una pregunta correctamente";
                if (HttpMethods.IsPut(metodo) || HttpMethods.IsPatch(metodo)) return "Usuario modificó una pregunta correctamente";
                if (HttpMethods.IsDelete(metodo)) return "Usuario eliminó una pregunta correctamente";
            }
            if (ruta.Contains("/pistas", StringComparison.Ordinal))
            {
                if (HttpMethods.IsPost(metodo)) return "Usuario agregó una pista correctamente";
                if (HttpMethods.IsPut(metodo) || HttpMethods.IsPatch(metodo)) return "Usuario modificó una pista correctamente";
                if (HttpMethods.IsDelete(metodo)) return "Usuario eliminó una pista correctamente";
            }
            if (ruta.Contains("/etapas", StringComparison.Ordinal))
            {
                if (HttpMethods.IsPost(metodo)) return "Usuario agregó una etapa correctamente";
                if (HttpMethods.IsDelete(metodo)) return "Usuario eliminó una etapa correctamente";
            }

            // Trivias.
            if (HttpMethods.IsPost(metodo) && EsRutaBase(ruta, "trivias"))
                return "Usuario creó una trivia correctamente";
            if (EsRutaRecurso(ruta, "trivias"))
            {
                if (ruta.EndsWith("/activar", StringComparison.Ordinal)) return "Usuario activó una trivia correctamente";
                if (HttpMethods.IsPut(metodo)) return "Usuario modificó una trivia correctamente";
                if (HttpMethods.IsDelete(metodo)) return "Usuario eliminó o desactivó una trivia correctamente";
            }

            // Búsquedas del tesoro.
            if (HttpMethods.IsPost(metodo) && EsRutaBase(ruta, "busquedas"))
                return "Usuario creó una búsqueda del tesoro correctamente";
            if (EsRutaRecurso(ruta, "busquedas"))
            {
                if (ruta.EndsWith("/activar", StringComparison.Ordinal)) return "Usuario activó una búsqueda del tesoro correctamente";
                if (HttpMethods.IsPut(metodo) || HttpMethods.IsPatch(metodo)) return "Usuario modificó una búsqueda del tesoro correctamente";
                if (HttpMethods.IsDelete(metodo)) return "Usuario eliminó o desactivó una búsqueda del tesoro correctamente";
            }

            // Misiones.
            if (HttpMethods.IsPost(metodo) && EsRutaBase(ruta, "misiones"))
                return "Usuario creó una misión correctamente";
            if (EsRutaRecurso(ruta, "misiones"))
            {
                if (ruta.EndsWith("/activar", StringComparison.Ordinal)) return "Usuario activó una misión correctamente";
                if (HttpMethods.IsPut(metodo) || HttpMethods.IsPatch(metodo)) return "Usuario modificó una misión correctamente";
                if (HttpMethods.IsDelete(metodo)) return "Usuario eliminó o desactivó una misión correctamente";
            }

            return "Solicitud procesada correctamente";
        }

        return ObtenerDescripcionGenerica(codigo);
    }

    private static bool EsRutaRecurso(string ruta, string recurso) =>
        EsRutaBase(ruta, recurso)
        || ruta.StartsWith($"/api/{recurso}/", StringComparison.Ordinal)
        || ruta.StartsWith($"/api/juegos/{recurso}/", StringComparison.Ordinal);

    private static bool EsRutaBase(string ruta, string recurso) =>
        ruta.Equals($"/api/{recurso}", StringComparison.Ordinal)
        || ruta.Equals($"/api/juegos/{recurso}", StringComparison.Ordinal);

    private static string? ObtenerDescripcionError(int codigo) => codigo switch
    {
        StatusCodes.Status400BadRequest => "Solicitud rechazada por validación",
        StatusCodes.Status401Unauthorized => "Solicitud rechazada por falta de autenticación",
        StatusCodes.Status403Forbidden => "Solicitud rechazada por falta de autorización",
        StatusCodes.Status404NotFound => "Recurso solicitado no encontrado",
        StatusCodes.Status409Conflict => "Solicitud rechazada por conflicto de negocio",
        StatusCodes.Status422UnprocessableEntity => "Solicitud rechazada por regla de negocio",
        >= StatusCodes.Status500InternalServerError =>
            "Solicitud falló por error interno del servidor",
        _ => null
    };

    private static string ObtenerDescripcionGenerica(int codigo) => codigo switch
    {
        >= 300 and < 400 => "Solicitud redirigida",
        >= 400 and < 500 => "Solicitud rechazada",
        >= 500 => "Solicitud falló por error del servidor",
        _ => "Solicitud procesada"
    };

    private static string? ObtenerPrimerClaim(
        ClaimsPrincipal usuario, params string[] tipos) =>
        tipos
            .Select(tipo => usuario.FindFirst(tipo)?.Value)
            .FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor));

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
            // Un claim mal formado no debe interrumpir la solicitud ni su registro técnico.
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
