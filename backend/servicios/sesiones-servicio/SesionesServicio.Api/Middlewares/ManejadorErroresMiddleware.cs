using System.Net;
using System.Text.Json;
using SesionesServicio.Api.Configuraciones;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Api.Middlewares;

// Centraliza el mapeo Excepción → respuesta HTTP. Las excepciones
// "esperadas" del dominio/aplicación se traducen a 4xx con un cuerpo
// { codigo, mensaje[, errores] } consistente con el resto de los
// microservicios del proyecto. Cualquier excepción no controlada se
// reduce a un 500 con cuerpo genérico para no filtrar internos.
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
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest, new
            {
                codigo = "VALIDACION",
                mensaje = ex.Message,
                errores = ex.Errores.Select(e => new { campo = e.Campo, mensaje = e.Mensaje })
            });
        }
        catch (SesionInvalidaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.BadRequest, "SESION_INVALIDA", ex.Message);
        }
        catch (ContenidoJuegoNoEncontradoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "CONTENIDO_NO_ENCONTRADO", ex.Message);
        }
        catch (ContenidoJuegoNoActivoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "CONTENIDO_NO_ACTIVO", ex.Message);
        }
        catch (UsuarioNoAutorizadoCrearSesionExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Forbidden,
                "USUARIO_NO_AUTORIZADO", ex.Message);
        }
        catch (SesionNoEncontradaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "SESION_NO_ENCONTRADA", ex.Message);
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
        => EscribirJsonAsync(contexto, estado, new { codigo, mensaje });

    private static Task EscribirJsonAsync(HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
