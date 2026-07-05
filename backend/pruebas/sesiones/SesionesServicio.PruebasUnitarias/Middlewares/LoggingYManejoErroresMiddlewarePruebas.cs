using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SesionesServicio.Presentacion.Middlewares;

namespace SesionesServicio.PruebasUnitarias.Middlewares;

public sealed class LoggingYManejoErroresMiddlewarePruebas
{
    [Fact]
    public async Task Logging_ConCorrelationId_LoConservaEnContextoYRespuesta()
    {
        const string correlationId = "correlacion-sesiones";
        var contexto = CrearContexto();
        contexto.Request.Headers[LoggingSolicitudesMiddleware.HeaderCorrelacion] = correlationId;
        var siguienteEjecutado = false;
        var middleware = new LoggingSolicitudesMiddleware(
            async ctx =>
            {
                siguienteEjecutado = true;
                await ctx.Response.StartAsync();
            },
            NullLogger<LoggingSolicitudesMiddleware>.Instance);

        await middleware.Invoke(contexto);

        Assert.True(siguienteEjecutado);
        Assert.Equal(
            correlationId,
            contexto.Items[LoggingSolicitudesMiddleware.ItemCorrelacion]);
        Assert.Equal(
            correlationId,
            contexto.Response.Headers[LoggingSolicitudesMiddleware.HeaderCorrelacion].ToString());
    }

    [Fact]
    public async Task Logging_SinCorrelationId_GeneraUnoYLoDevuelve()
    {
        var contexto = CrearContexto();
        var middleware = new LoggingSolicitudesMiddleware(
            ctx => ctx.Response.StartAsync(),
            NullLogger<LoggingSolicitudesMiddleware>.Instance);

        await middleware.Invoke(contexto);

        var correlationId =
            contexto.Items[LoggingSolicitudesMiddleware.ItemCorrelacion] as string;
        Assert.True(Guid.TryParse(correlationId, out _));
        Assert.Equal(
            correlationId,
            contexto.Response.Headers[LoggingSolicitudesMiddleware.HeaderCorrelacion].ToString());
    }

    [Fact]
    public async Task Logging_ConClaimsDirectos_RegistraUsuarioYRol()
    {
        var contexto = CrearContexto();
        contexto.User = CrearUsuario(
            new Claim(ClaimTypes.NameIdentifier, "usuario-789"),
            new Claim("preferred_username", "participante1"),
            new Claim(ClaimTypes.Role, "Participante"));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        Assert.Equal("usuario-789", registro.Valores["UsuarioId"]);
        Assert.Equal("participante1", registro.Valores["Usuario"]);
        Assert.Equal("Participante", registro.Valores["Rol"]);
    }

    [Fact]
    public async Task Logging_ConRealmAccess_RegistraRolDeNegocio()
    {
        var contexto = CrearContexto();
        contexto.User = CrearUsuario(
            new Claim("realm_access", """{"roles":["Participante"]}"""));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        Assert.Equal("Participante", registro.Valores["Rol"]);
    }

    [Fact]
    public async Task Logging_ConRealmAccessInvalido_NoFalla()
    {
        var contexto = CrearContexto();
        contexto.User = CrearUsuario(new Claim("realm_access", "{invalido"));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        // Usuario autenticado pero sin rol de negocio resoluble: NoDisponible, nunca null.
        Assert.Equal("NoDisponible", registro.Valores["Rol"]);
    }

    [Fact]
    public async Task Logging_ConRealmAccessDeFormaIncorrecta_NoFalla()
    {
        var contexto = CrearContexto();
        contexto.User = CrearUsuario(new Claim("realm_access", "[]"));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        // Usuario autenticado pero sin rol de negocio resoluble: NoDisponible, nunca null.
        Assert.Equal("NoDisponible", registro.Valores["Rol"]);
    }

    [Fact]
    public async Task Logging_SinToken_UsaAnonimoNoNull()
    {
        var contexto = CrearContexto();
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        Assert.Equal("Anonimo", registro.Valores["UsuarioId"]);
        Assert.Equal("Anonimo", registro.Valores["Usuario"]);
        Assert.Equal("Anonimo", registro.Valores["Rol"]);
    }

    [Fact]
    public async Task Logging_ConPreferredUsername_ResuelveUsuario()
    {
        var contexto = CrearContexto();
        contexto.User = CrearUsuario(
            new Claim("preferred_username", "participante42"));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        Assert.Equal("participante42", registro.Valores["Usuario"]);
    }

    [Fact]
    public async Task Logging_AbandonarSesion_RegistraDescripcionLegible()
    {
        var contexto = CrearContexto();
        contexto.Request.Method = HttpMethods.Delete;
        contexto.Request.Path = $"/api/sesiones/{Guid.NewGuid()}/abandonar";
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro, StatusCodes.Status204NoContent);

        Assert.Equal(
            "Usuario abandonó la sesión correctamente",
            registro.Valores["Descripcion"]);
    }

    [Fact]
    public async Task Logging_ConsultaSesionesDisponibles_RegistraDescripcionLegible()
    {
        var contexto = CrearContexto();
        contexto.Request.Method = HttpMethods.Get;
        contexto.Request.Path = "/api/sesiones/participante/disponibles";
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        Assert.Equal(
            "Usuario consultó sesiones disponibles",
            registro.Valores["Descripcion"]);
    }

    [Theory]
    [InlineData(
        StatusCodes.Status403Forbidden,
        "Solicitud rechazada por falta de autorización")]
    [InlineData(
        StatusCodes.Status500InternalServerError,
        "Solicitud falló por error interno del servidor")]
    public async Task Logging_RespuestaError_RegistraDescripcionLegible(
        int codigoEstado, string descripcion)
    {
        var contexto = CrearContexto();
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro, codigoEstado);

        Assert.Equal(descripcion, registro.Valores["Descripcion"]);
    }

    [Fact]
    public async Task Errores_ErrorNoControlado_DevuelveCodigoYCorrelationIdSinDetalleInterno()
    {
        const string correlationId = "error-sesiones";
        var contexto = CrearContexto(correlationId);
        var middleware = new ManejadorErroresMiddleware(
            _ => throw new InvalidOperationException("detalle interno sensible"),
            NullLogger<ManejadorErroresMiddleware>.Instance);

        await middleware.Invoke(contexto);

        var cuerpo = await LeerCuerpoAsync(contexto);
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
        Assert.Equal("ERROR_INTERNO", json.RootElement.GetProperty("codigo").GetString());
        Assert.Equal(
            correlationId,
            json.RootElement.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("detalle interno sensible", cuerpo);
        Assert.DoesNotContain("stack", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Errores_JsonInvalido_DevuelveValidacionYCorrelationId()
    {
        const string correlationId = "json-sesiones";
        var contexto = CrearContexto(correlationId);
        var middleware = new ManejadorErroresMiddleware(
            _ => throw new JsonException("json inválido", "$.fechaInicio", null, null),
            NullLogger<ManejadorErroresMiddleware>.Instance);

        await middleware.Invoke(contexto);

        using var json = JsonDocument.Parse(await LeerCuerpoAsync(contexto));
        Assert.Equal(StatusCodes.Status400BadRequest, contexto.Response.StatusCode);
        Assert.Equal("VALIDACION", json.RootElement.GetProperty("codigo").GetString());
        Assert.Equal(
            correlationId,
            json.RootElement.GetProperty("correlationId").GetString());
    }

    private static DefaultHttpContext CrearContexto(string? correlationId = null)
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();
        if (correlationId is not null)
            contexto.Items[LoggingSolicitudesMiddleware.ItemCorrelacion] = correlationId;
        return contexto;
    }

    private static ClaimsPrincipal CrearUsuario(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Pruebas"));

    private static async Task EjecutarLoggingAsync(
        HttpContext contexto,
        RegistroPrueba<LoggingSolicitudesMiddleware> registro,
        int codigoEstado = StatusCodes.Status200OK)
    {
        var middleware = new LoggingSolicitudesMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = codigoEstado;
                return Task.CompletedTask;
            },
            registro);

        await middleware.Invoke(contexto);
    }

    private static async Task<string> LeerCuerpoAsync(HttpContext contexto)
    {
        contexto.Response.Body.Position = 0;
        using var lector = new StreamReader(contexto.Response.Body, leaveOpen: true);
        return await lector.ReadToEndAsync();
    }

    private sealed class RegistroPrueba<T> : ILogger<T>
    {
        public IReadOnlyDictionary<string, object?> Valores { get; private set; } =
            new Dictionary<string, object?>();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            AlcanceNulo.Instancia;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> valores)
                Valores = valores.ToDictionary(par => par.Key, par => par.Value);
        }
    }

    private sealed class AlcanceNulo : IDisposable
    {
        public static AlcanceNulo Instancia { get; } = new();
        public void Dispose() { }
    }
}
