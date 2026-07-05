using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SesionesServicio.Infraestructura.Logging;

namespace SesionesServicio.PruebasUnitarias.Logging;

public sealed class RegistroLogsAplicacionDotNetPruebas
{
    [Fact]
    public void Informacion_ImprimeEventoDescripcionYPropiedades()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());

        adaptador.Informacion(
            "SesionCreada",
            "Operador creó una sesión correctamente",
            new Dictionary<string, object?> { ["SesionId"] = "abc" });

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.Contains("EventoAplicacion=SesionCreada", mensaje);
        Assert.Contains("Descripcion=Operador creó una sesión correctamente", mensaje);
        Assert.Contains("\"SesionId\":\"abc\"", mensaje);
    }

    [Fact]
    public void Informacion_RedactaClavesSensibles()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());

        adaptador.Informacion(
            "UsuarioCreado",
            "desc",
            new Dictionary<string, object?>
            {
                ["ContrasenaTemporal"] = "SECRETO123",
                ["Token"] = "eyJhbGciOi",
                ["UsuarioId"] = "u-1"
            });

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.DoesNotContain("SECRETO123", mensaje);
        Assert.DoesNotContain("eyJhbGciOi", mensaje);
        Assert.Contains("***REDACTADO***", mensaje);
        Assert.Contains("\"UsuarioId\":\"u-1\"", mensaje);
    }

    [Fact]
    public void Informacion_RedactaClavesSensiblesTambienEnScope()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());

        adaptador.Informacion(
            "UsuarioCreado",
            "desc",
            new Dictionary<string, object?>
            {
                ["ContrasenaTemporal"] = "SECRETO123",
                ["UsuarioId"] = "u-1"
            });

        // El scope (IncludeScopes = true en consola/Docker) debe recibir las
        // propiedades ya redactadas, nunca el valor sensible original.
        var scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(logger.UltimoScope);
        Assert.Equal("***REDACTADO***", scope["ContrasenaTemporal"]);
        Assert.Equal("u-1", scope["UsuarioId"]);
    }

    [Fact]
    public void SinHttpContext_ActorEsAnonimo()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());

        adaptador.Informacion("EventoX", "desc");

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.Contains("ActorId=Anonimo", mensaje);
        Assert.Contains("ActorUsuario=Anonimo", mensaje);
        Assert.Contains("ActorRol=Anonimo", mensaje);
    }

    [Fact]
    public void ConUsuarioAutenticado_IncluyeActorYCorrelationId()
    {
        var contexto = new DefaultHttpContext();
        contexto.Items["CorrelationId"] = "corr-123";
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "kc-1"),
                new Claim("preferred_username", "operador1"),
                new Claim(ClaimTypes.Role, "Operador")
            },
            "Pruebas"));

        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, ConContexto(contexto));

        adaptador.Informacion("SesionCreada", "desc");

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.Contains("CorrelationId=corr-123", mensaje);
        Assert.Contains("ActorId=kc-1", mensaje);
        Assert.Contains("ActorUsuario=operador1", mensaje);
        Assert.Contains("ActorRol=Operador", mensaje);
    }

    private static IHttpContextAccessor SinContexto() =>
        Mock.Of<IHttpContextAccessor>(a => a.HttpContext == null);

    private static IHttpContextAccessor ConContexto(HttpContext contexto) =>
        Mock.Of<IHttpContextAccessor>(a => a.HttpContext == contexto);

    private sealed class LoggerCaptura : ILogger<RegistroLogsAplicacionDotNet>
    {
        public List<string> Mensajes { get; } = new();
        public object? UltimoScope { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            UltimoScope = state;
            return AlcanceNulo.Instancia;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Mensajes.Add(formatter(state, exception));

        private sealed class AlcanceNulo : IDisposable
        {
            public static readonly AlcanceNulo Instancia = new();
            public void Dispose() { }
        }
    }
}
