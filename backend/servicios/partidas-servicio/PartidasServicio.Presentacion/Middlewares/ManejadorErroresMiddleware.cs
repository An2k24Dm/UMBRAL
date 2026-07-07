using System.Net;
using System.Text.Json;
using PartidasServicio.Aplicacion.Validaciones;
using PartidasServicio.Dominio.Excepciones;
using PartidasServicio.Presentacion.Configuraciones;

namespace PartidasServicio.Presentacion.Middlewares;

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
            _registro.LogWarning("Solicitud rechazada por validación: {Mensaje}", ex.Message);
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest, new
            {
                codigo = "VALIDACION",
                mensaje = ex.Message,
                errores = ex.Errores.Select(e => new { campo = e.Campo, mensaje = e.Mensaje }),
                correlationId = ObtenerCorrelationId(contexto)
            });
        }
        catch (SesionNoActivaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.UnprocessableEntity,
                "SESION_NO_ACTIVA", ex.Message);
        }
        catch (ParticipanteNoEnSesionExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Forbidden,
                "PARTICIPANTE_NO_EN_SESION", ex.Message);
        }
        catch (PreguntaNoEncontradaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "PREGUNTA_NO_ENCONTRADA", ex.Message);
        }
        catch (PartidaNoEncontradaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "PARTIDA_NO_ENCONTRADA", ex.Message);
        }
        catch (TransicionEstadoInvalidaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.UnprocessableEntity,
                "TRANSICION_ESTADO_INVALIDA", ex.Message);
        }
        catch (JsonException ex)
        {
            var correlationId = ObtenerCorrelationId(contexto);
            _registro.LogWarning(ex, "Solicitud rechazada por JSON inválido. CorrelationId={CorrelationId}",
                correlationId);
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest,
                RespuestaErrorModelo.ConstruirDesdeJsonException(ex, correlationId));
        }
        catch (BadHttpRequestException ex) when (ex.InnerException is JsonException jsonInner)
        {
            var correlationId = ObtenerCorrelationId(contexto);
            _registro.LogWarning(ex, "Solicitud rechazada por JSON inválido. CorrelationId={CorrelationId}",
                correlationId);
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest,
                RespuestaErrorModelo.ConstruirDesdeJsonException(jsonInner, correlationId));
        }
        catch (Exception ex)
        {
            _registro.LogError(ex, "Error no controlado.");
            await EscribirCodigoAsync(contexto, HttpStatusCode.InternalServerError,
                "ERROR_INTERNO", "Ocurrió un error inesperado en el servidor.");
        }
    }

    private Task EscribirCodigoAsync(
        HttpContext contexto, HttpStatusCode estado, string codigo, string mensaje)
    {
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

    private static string? ObtenerCorrelationId(HttpContext contexto)
        => contexto.Items.TryGetValue("CorrelationId", out var valor) ? valor as string : null;

    private static Task EscribirJsonAsync(HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
