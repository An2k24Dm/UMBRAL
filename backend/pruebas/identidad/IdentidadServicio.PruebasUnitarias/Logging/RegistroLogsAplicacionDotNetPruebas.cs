using System.Security.Claims;
using IdentidadServicio.Infraestructura.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.PruebasUnitarias.Logging;

public sealed class RegistroLogsAplicacionDotNetPruebas
{
    [Fact]
    public void Informacion_RedactaPropiedadesSensiblesEnMensajeYScope()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());

        adaptador.Informacion(
            "UsuarioCreado",
            "desc",
            new Dictionary<string, object?>
            {
                ["passwordTemporal"] = "secreto",
                ["UsuarioId"] = "u-1"
            });

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.DoesNotContain("secreto", mensaje);
        Assert.Contains("***REDACTADO***", mensaje);
        Assert.Contains("\"UsuarioId\":\"u-1\"", mensaje);
        var scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(logger.UltimoScope);
        scope["passwordTemporal"].Should().Be("***REDACTADO***");
    }

    [Fact]
    public void SinHttpContext_RegistraActorAnonimo()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());

        adaptador.Advertencia("Evento", "desc");

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.Contains("CorrelationId=NoDisponible", mensaje);
        Assert.Contains("ActorId=Anonimo", mensaje);
        Assert.Contains("ActorUsuario=Anonimo", mensaje);
        Assert.Contains("ActorRol=Anonimo", mensaje);
    }

    [Fact]
    public void UsuarioAutenticado_UsaClaimsAlternativosYRolesNormalizados()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers["X-Correlation-Id"] = "corr-identidad";
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("nameid", "usuario-1"),
                new Claim("name", "operador"),
                new Claim("role", "Operador;Participante"),
                new Claim("realm_access", "{\"roles\":[\"Administrador\",\"desconocido\"]}")
            },
            "Pruebas"));
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, ConContexto(contexto));

        adaptador.Informacion("Evento", "desc");

        var mensaje = Assert.Single(logger.Mensajes);
        Assert.Contains("CorrelationId=corr-identidad", mensaje);
        Assert.Contains("ActorId=usuario-1", mensaje);
        Assert.Contains("ActorUsuario=operador", mensaje);
        Assert.Contains("ActorRol=Operador,Participante,Administrador", mensaje);
    }

    [Fact]
    public void RealmAccessInvalido_NoRompeLogging()
    {
        var contexto = new DefaultHttpContext();
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("realm_access", "no-json") },
            "Pruebas"));
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, ConContexto(contexto));

        adaptador.Informacion("Evento", "desc");

        Assert.Contains("ActorRol=NoDisponible", Assert.Single(logger.Mensajes));
    }

    [Fact]
    public void Error_RegistraNivelYExcepcion()
    {
        var logger = new LoggerCaptura();
        var adaptador = new RegistroLogsAplicacionDotNet(logger, SinContexto());
        var excepcion = new InvalidOperationException("boom");

        adaptador.Error(excepcion, "EventoError", "falló");

        logger.UltimoNivel.Should().Be(LogLevel.Error);
        logger.UltimaExcepcion.Should().BeSameAs(excepcion);
    }

    private static IHttpContextAccessor SinContexto() =>
        Mock.Of<IHttpContextAccessor>(a => a.HttpContext == null);

    private static IHttpContextAccessor ConContexto(HttpContext contexto) =>
        Mock.Of<IHttpContextAccessor>(a => a.HttpContext == contexto);

    private sealed class LoggerCaptura : ILogger<RegistroLogsAplicacionDotNet>
    {
        public List<string> Mensajes { get; } = new();
        public object? UltimoScope { get; private set; }
        public LogLevel UltimoNivel { get; private set; }
        public Exception? UltimaExcepcion { get; private set; }

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
        {
            UltimoNivel = logLevel;
            UltimaExcepcion = exception;
            Mensajes.Add(formatter(state, exception));
        }

        private sealed class AlcanceNulo : IDisposable
        {
            public static readonly AlcanceNulo Instancia = new();
            public void Dispose() { }
        }
    }
}
