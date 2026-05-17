using System.Net;
using System.Text.Json;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Api.Middlewares;

public sealed class ManejadorErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorErroresMiddleware> _registro;

    public ManejadorErroresMiddleware(
        RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    public async Task Invoke(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (CuentaDesactivadaExcepcion ex)
        {
            await EscribirAsync(contexto, HttpStatusCode.Forbidden, "CUENTA_DESACTIVADA", ex.Message);
        }
        catch (AccesoNoPermitidoExcepcion ex)
        {
            await EscribirAsync(contexto, HttpStatusCode.Forbidden, "ACCESO_NO_PERMITIDO", ex.Message);
        }
        catch (RolNoValidoExcepcion ex)
        {
            await EscribirAsync(contexto, HttpStatusCode.Forbidden, "ROL_NO_VALIDO", ex.Message);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
        {
            // En HU01 reutilizamos para "credenciales inválidas" (401) y datos inválidos (400).
            var codigo = ex.Message.Contains("Credenciales", StringComparison.OrdinalIgnoreCase)
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.BadRequest;
            await EscribirAsync(contexto, codigo, "DATOS_INVALIDOS", ex.Message);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex, "Error no controlado.");
            await EscribirAsync(contexto, HttpStatusCode.InternalServerError,
                "ERROR_INTERNO", "Ocurrió un error inesperado en el servidor.");
        }
    }

    private static Task EscribirAsync(HttpContext contexto, HttpStatusCode estado, string codigo, string mensaje)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        var cuerpo = JsonSerializer.Serialize(new { codigo, mensaje },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return contexto.Response.WriteAsync(cuerpo);
    }
}
