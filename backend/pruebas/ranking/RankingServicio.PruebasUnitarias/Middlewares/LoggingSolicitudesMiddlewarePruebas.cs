using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RankingServicio.Presentacion.Middlewares;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Middlewares;

public sealed class LoggingSolicitudesMiddlewarePruebas
{
    [Fact]
    public async Task ConCorrelationId_LoConservaEnContextoYRespuesta()
    {
        const string correlationId = "correlacion-ranking";
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
    public async Task SinCorrelationId_GeneraUnoYLoDevuelve()
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
    public async Task RegistraCodigoEstadoFinalDeLaRespuesta()
    {
        var contexto = CrearContexto();
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarAsync(
            contexto, registro,
            metodo: HttpMethods.Get,
            ruta: "/api/ranking/global",
            codigoEstado: StatusCodes.Status200OK);

        Assert.Equal(StatusCodes.Status200OK, Convert.ToInt32(registro.Valores["CodigoEstado"]));
    }

    [Fact]
    public async Task ConClaimsDirectos_RegistraUsuarioYRol()
    {
        var contexto = CrearContexto();
        contexto.User = CrearUsuario(
            new Claim(ClaimTypes.NameIdentifier, "usuario-321"),
            new Claim("preferred_username", "participante7"),
            new Claim(ClaimTypes.Role, "Participante"));
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarAsync(contexto, registro);

        Assert.Equal("usuario-321", registro.Valores["UsuarioId"]);
        Assert.Equal("participante7", registro.Valores["Usuario"]);
        Assert.Equal("Participante", registro.Valores["Rol"]);
    }

    [Fact]
    public async Task SinToken_UsaAnonimoNoNull()
    {
        var contexto = CrearContexto();
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarAsync(contexto, registro);

        Assert.Equal("Anonimo", registro.Valores["UsuarioId"]);
        Assert.Equal("Anonimo", registro.Valores["Usuario"]);
        Assert.Equal("Anonimo", registro.Valores["Rol"]);
    }

    [Theory]
    [InlineData(
        "/api/ranking/sesiones/8f9d2c1e-0000-0000-0000-000000000001/participantes",
        "Usuario consultó ranking de participantes de una sesión")]
    [InlineData(
        "/api/ranking/sesiones/8f9d2c1e-0000-0000-0000-000000000001/equipos",
        "Usuario consultó ranking de equipos de una sesión")]
    [InlineData(
        "/api/ranking/global",
        "Usuario consultó ranking global")]
    public async Task RutasDeConsulta_RegistranDescripcionLegible(
        string ruta, string descripcion)
    {
        var contexto = CrearContexto();
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarAsync(contexto, registro, HttpMethods.Get, ruta);

        Assert.Equal(descripcion, registro.Valores["Descripcion"]);
    }

    [Theory]
    [InlineData(
        StatusCodes.Status403Forbidden,
        "Solicitud rechazada por falta de autorización")]
    [InlineData(
        StatusCodes.Status500InternalServerError,
        "Solicitud falló por error interno del servidor")]
    public async Task RespuestaError_RegistraDescripcionLegible(
        int codigoEstado, string descripcion)
    {
        var contexto = CrearContexto();
        var registro = new RegistroPrueba<LoggingSolicitudesMiddleware>();

        await EjecutarAsync(
            contexto, registro,
            HttpMethods.Get, "/api/ranking/global", codigoEstado);

        Assert.Equal(descripcion, registro.Valores["Descripcion"]);
    }

    private static DefaultHttpContext CrearContexto()
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();
        return contexto;
    }

    private static ClaimsPrincipal CrearUsuario(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Pruebas"));

    private static async Task EjecutarAsync(
        HttpContext contexto,
        RegistroPrueba<LoggingSolicitudesMiddleware> registro,
        string metodo = "GET",
        string ruta = "/api/ranking/global",
        int codigoEstado = StatusCodes.Status200OK)
    {
        contexto.Request.Method = metodo;
        contexto.Request.Path = ruta;
        var middleware = new LoggingSolicitudesMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = codigoEstado;
                return Task.CompletedTask;
            },
            registro);

        await middleware.Invoke(contexto);
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
