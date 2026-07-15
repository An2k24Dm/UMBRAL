using System.Net;
using System.Text.Json;

namespace RankingServicio.Presentacion.Middlewares;

public sealed class ManejadorErroresMiddleware
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorErroresMiddleware> _registro;

    public ManejadorErroresMiddleware(
        RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    private static string? ObtenerCorrelationId(HttpContext contexto)
        => contexto.Items.TryGetValue(
            LoggingSolicitudesMiddleware.ItemCorrelacion, out var valor)
            ? valor as string
            : null;

    public async Task Invoke(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (Exception ex)
        {
            var correlationId = ObtenerCorrelationId(contexto);
            _registro.LogError(
                ex,
                "Error no controlado en ranking-servicio. CorrelationId={CorrelationId}",
                correlationId);
            contexto.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            contexto.Response.ContentType = "application/json";
            await contexto.Response.WriteAsync(
                JsonSerializer.Serialize(
                    new
                    {
                        codigo = "ERROR_INTERNO",
                        mensaje = "Ocurrió un error inesperado.",
                        correlationId
                    },
                    OpcionesJson));
        }
    }
}
