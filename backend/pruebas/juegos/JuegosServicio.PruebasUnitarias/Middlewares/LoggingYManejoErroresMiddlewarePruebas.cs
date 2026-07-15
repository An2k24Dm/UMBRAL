using System.Text.Json;
using System.Security.Claims;
using JuegosServicio.Presentacion.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JuegosServicio.PruebasUnitarias.Middlewares;

public sealed class LoggingYManejoErroresMiddlewarePruebas
{
    [Fact]
    public async Task Logging_ConCorrelationId_LoConservaEnContextoYRespuesta()
    {
        const string correlationId = "correlacion-juegos";
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
            new Claim(ClaimTypes.NameIdentifier, "usuario-456"),
            new Claim("preferred_username", "operador1"),
            new Claim(ClaimTypes.Role, "Operador"));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro);

        Assert.Equal("usuario-456", registro.Valores["UsuarioId"]);
        Assert.Equal("operador1", registro.Valores["Usuario"]);
        Assert.Equal("Operador", registro.Valores["Rol"]);
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
    public async Task Logging_CreacionTriviaExitosa_RegistraDescripcionLegible()
    {
        var contexto = CrearContexto();
        contexto.Request.Method = HttpMethods.Post;
        contexto.Request.Path = "/api/juegos/trivias";
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarLoggingAsync(contexto, registro, StatusCodes.Status201Created);

        Assert.Equal(
            "Usuario creó una trivia correctamente",
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
        const string correlationId = "error-juegos";
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
        const string correlationId = "json-juegos";
        var contexto = CrearContexto(correlationId);
        var middleware = new ManejadorErroresMiddleware(
            _ => throw new JsonException("json inválido", "$.tiempo", null, null),
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
