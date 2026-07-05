using System.Net;
using System.Text.Json;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Presentacion.Middlewares;

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
            _registro.LogWarning(
                "Solicitud rechazada por validación: {Mensaje}", ex.Message);
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest, new
            {
                codigo = "VALIDACION",
                mensaje = ex.Message,
                errores = ex.Errores.Select(e => new { campo = e.Campo, mensaje = e.Mensaje }),
                correlationId = ObtenerCorrelationId(contexto)
            });
        }
        catch (ExcepcionNoEncontrado ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "NO_ENCONTRADO", ex.Message);
        }
        catch (ContenidoUsadoEnMisionActivaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.UnprocessableEntity,
                "CONTENIDO_USADO_EN_MISION_ACTIVA", ex.Message);
        }
        catch (MisionConSesionesVigentesExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.UnprocessableEntity,
                "MISION_CON_SESIONES_VIGENTES", ex.Message);
        }
        catch (ContenidoConSesionesVigentesExcepcion ex)
        {
            // Subclase específica de ExcepcionDominio: usamos un código
            // dedicado para que el frontend pueda reconocer y mostrar el
            // mensaje exacto, sin pasarse por el catch genérico de abajo.
            await EscribirCodigoAsync(contexto, HttpStatusCode.UnprocessableEntity,
                "CONTENIDO_CON_SESIONES_VIGENTES", ex.Message);
        }
        catch (ExcepcionDominio ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.UnprocessableEntity,
                "REGLA_NEGOCIO", ex.Message);
        }
        catch (JsonException ex)
        {
            await EscribirErrorJsonAsync(contexto, ex);
        }
        catch (BadHttpRequestException ex) when (ex.InnerException is JsonException jsonInner)
        {
            await EscribirErrorJsonAsync(contexto, jsonInner, ex);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex, "Error no controlado en juegos-servicio.");
            await EscribirCodigoAsync(contexto, HttpStatusCode.InternalServerError,
                "ERROR_INTERNO", "Ocurrió un error inesperado en el servidor.");
        }
    }

    private Task EscribirCodigoAsync(
        HttpContext contexto, HttpStatusCode estado, string codigo, string mensaje)
    {
        // Los rechazos por reglas de negocio quedan como warning; los errores
        // no controlados ya se registran como LogError.
        if ((int)estado < 500)
            _registro.LogWarning(
                "Solicitud rechazada con {CodigoError} ({CodigoEstado}): {Mensaje}",
                codigo, (int)estado, mensaje);

        return EscribirJsonAsync(contexto, estado, new
        {
            codigo,
            mensaje,
            correlationId = ObtenerCorrelationId(contexto)
        });
    }

    private Task EscribirErrorJsonAsync(
        HttpContext contexto, JsonException json, Exception? excepcionRegistro = null)
    {
        var correlationId = ObtenerCorrelationId(contexto);
        _registro.LogWarning(
            excepcionRegistro ?? json,
            "Solicitud rechazada por JSON inválido. CorrelationId={CorrelationId}",
            correlationId);

        return EscribirJsonAsync(contexto, HttpStatusCode.BadRequest, new
        {
            codigo = "VALIDACION",
            mensaje = "El cuerpo de la solicitud tiene un formato inválido.",
            errores = new[]
            {
                new
                {
                    campo = json.Path ?? "solicitud",
                    mensaje = "El valor proporcionado no tiene un formato válido."
                }
            },
            correlationId
        });
    }

    private static string? ObtenerCorrelationId(HttpContext contexto)
        => contexto.Items.TryGetValue("CorrelationId", out var valor) ? valor as string : null;

    private static Task EscribirJsonAsync(HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
