using System.Net;
using System.Security.Claims;
using System.Text.Json;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Presentacion.Middlewares;

public sealed class BloqueoUsuarioInactivoMiddleware
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<BloqueoUsuarioInactivoMiddleware> _registro;

    public BloqueoUsuarioInactivoMiddleware(
        RequestDelegate siguiente,
        ILogger<BloqueoUsuarioInactivoMiddleware> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    public async Task Invoke(
        HttpContext contexto,
        IValidadorAccesoUsuarioActivo validadorAcceso)
    {
        var principal = contexto.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var idKeycloak = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? principal.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(idKeycloak))
            {
                var resultado = await validadorAcceso.ValidarAsync(
                    idKeycloak, contexto.RequestAborted);
                if (!resultado.PuedeAcceder)
                {
                    _registro.LogWarning(
                        "Petición bloqueada: usuario {IdKeycloak} sin acceso ({Codigo}) (HU12).",
                        idKeycloak, resultado.Codigo);
                    await EscribirJsonAsync(contexto, HttpStatusCode.Forbidden, new
                    {
                        codigo = resultado.Codigo,
                        mensaje = resultado.Mensaje
                    });
                    return;
                }
            }
        }

        await _siguiente(contexto);
    }

    private static Task EscribirJsonAsync(
        HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
