using System.Net;
using System.Text.Json;
using IdentidadServicio.Api.Configuraciones;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Dominio.Excepciones;
using Microsoft.AspNetCore.Http;

namespace IdentidadServicio.Api.Middlewares;

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

    public async Task Invoke(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (ExcepcionValidacion ex)
        {
            // HU02: errores por campo → HTTP 400 con { codigo, mensaje, errores }.
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest, new
            {
                codigo = "VALIDACION",
                mensaje = ex.Message,
                errores = ex.Errores.Select(e => new { campo = e.Campo, mensaje = e.Mensaje })
            });
        }
        catch (CuentaDesactivadaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Forbidden, "CUENTA_DESACTIVADA", ex.Message);
        }
        catch (AccesoNoPermitidoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Forbidden, "ACCESO_NO_PERMITIDO", ex.Message);
        }
        catch (RolNoValidoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Forbidden, "ROL_NO_VALIDO", ex.Message);
        }
        catch (DatosUsuarioInvalidosExcepcion ex)
        {
            var codigo = ex.Message.Contains("Credenciales", StringComparison.OrdinalIgnoreCase)
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.BadRequest;
            await EscribirCodigoAsync(contexto, codigo, "DATOS_INVALIDOS", ex.Message);
        }
        catch (JsonException ex)
        {
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest,
                RespuestaErrorModelo.ConstruirDesdeJsonException(ex));
        }
        catch (BadHttpRequestException ex) when (ex.InnerException is JsonException jsonInner)
        {
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest,
                RespuestaErrorModelo.ConstruirDesdeJsonException(jsonInner));
        }
        catch (Exception ex)
        {
            _registro.LogError(ex, "Error no controlado.");
            await EscribirCodigoAsync(contexto, HttpStatusCode.InternalServerError,
                "ERROR_INTERNO", "Ocurrió un error inesperado en el servidor.");
        }
    }

    private static Task EscribirCodigoAsync(
        HttpContext contexto, HttpStatusCode estado, string codigo, string mensaje)
    {
        return EscribirJsonAsync(contexto, estado, new { codigo, mensaje });
    }

    private static Task EscribirJsonAsync(HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
