using System.Diagnostics;
using Yarp.ReverseProxy.Transforms;

const string HeaderCorrelacion = "X-Correlation-Id";
const string ItemCorrelacion = "CorrelationId";
const string Servicio = "api-gateway";

var constructor = WebApplication.CreateBuilder(args);

constructor.Logging.ClearProviders();
constructor.Logging.AddSimpleConsole(opciones =>
{
    opciones.SingleLine = true;
    opciones.TimestampFormat = "HH:mm:ss ";
    opciones.IncludeScopes = true;
});
constructor.Logging.AddDebug();

constructor.Services.AddReverseProxy()
    .LoadFromConfig(constructor.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            if (transformContext.HttpContext.Items.TryGetValue(ItemCorrelacion, out var valor)
                && valor is string correlationId
                && !string.IsNullOrWhiteSpace(correlationId))
            {
                transformContext.ProxyRequest.Headers.Remove(HeaderCorrelacion);
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                    HeaderCorrelacion, correlationId);
            }

            return ValueTask.CompletedTask;
        });
    });

var aplicacion = constructor.Build();

var registro = aplicacion.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger(Servicio);

aplicacion.Use(async (contexto, siguiente) =>
{
    var recibido = contexto.Request.Headers[HeaderCorrelacion].ToString();
    var correlationId = string.IsNullOrWhiteSpace(recibido)
        ? Guid.NewGuid().ToString()
        : recibido;

    contexto.Items[ItemCorrelacion] = correlationId;
    contexto.Response.OnStarting(() =>
    {
        contexto.Response.Headers[HeaderCorrelacion] = correlationId;
        return Task.CompletedTask;
    });

    var esWebSocket = contexto.WebSockets.IsWebSocketRequest;
    var cronometro = Stopwatch.StartNew();

    using var _ = registro.BeginScope(new Dictionary<string, object>
    {
        ["Servicio"] = Servicio,
        ["CorrelationId"] = correlationId
    });

    try
    {
        await siguiente();
    }
    finally
    {
        cronometro.Stop();

        if (esWebSocket)
        {
            registro.LogInformation(
                "Servicio={Servicio}, Descripcion=Conexión WebSocket finalizada, {Metodo} {Ruta} cerró {CodigoEstado} tras {DuracionMs}ms. " +
                "Ip={Ip}, UserAgent={UserAgent}, CorrelationId={CorrelationId}",
                Servicio,
                contexto.Request.Method,
                contexto.Request.Path.Value,
                contexto.Response.StatusCode,
                cronometro.ElapsedMilliseconds,
                contexto.Connection.RemoteIpAddress?.ToString(),
                contexto.Request.Headers.UserAgent.ToString(),
                correlationId);
        }
        else
        {
            registro.LogInformation(
                "Servicio={Servicio}, Descripcion=Solicitud enrutada, HTTP {Metodo} {Ruta} respondió {CodigoEstado} en {DuracionMs}ms. " +
                "Ip={Ip}, UserAgent={UserAgent}, CorrelationId={CorrelationId}",
                Servicio,
                contexto.Request.Method,
                contexto.Request.Path.Value,
                contexto.Response.StatusCode,
                cronometro.ElapsedMilliseconds,
                contexto.Connection.RemoteIpAddress?.ToString(),
                contexto.Request.Headers.UserAgent.ToString(),
                correlationId);
        }
    }
});

aplicacion.UseWebSockets();
aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "api-gateway" }));
aplicacion.MapReverseProxy();

await aplicacion.RunAsync();
