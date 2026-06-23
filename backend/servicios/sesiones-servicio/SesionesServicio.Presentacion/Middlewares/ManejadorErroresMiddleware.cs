using System.Net;
using System.Text.Json;
using SesionesServicio.Presentacion.Configuraciones;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Presentacion.Middlewares;

// Centraliza el mapeo Excepción → respuesta HTTP. Las excepciones
// "esperadas" del dominio/aplicación se traducen a 4xx con un cuerpo
// { codigo, mensaje[, errores] } consistente con el resto del proyecto.
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
        catch (MisionNoEncontradaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "MISION_NO_ENCONTRADA", ex.Message);
        }
        catch (MisionNoActivaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "MISION_NO_ACTIVA", ex.Message);
        }
        catch (MisionSinEtapasExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "MISION_SIN_ETAPAS", ex.Message);
        }
        catch (EquipoInvalidoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "EQUIPO_INVALIDO", ex.Message);
        }
        catch (ParticipacionInvalidaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "PARTICIPACION_INVALIDA", ex.Message);
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
        catch (EquipoNoEncontradoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "EQUIPO_NO_ENCONTRADO", ex.Message);
        }
        catch (SesionNoGrupalExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "SESION_NO_GRUPAL", ex.Message);
        }
        catch (SesionNoModificableExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "SESION_NO_MODIFICABLE", ex.Message);
        }
        catch (SesionNoEliminableExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "SESION_NO_ELIMINABLE", ex.Message);
        }
        catch (AccesoSesionNoPermitidoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Forbidden,
                "ACCESO_SESION_NO_PERMITIDO", ex.Message);
        }
        catch (TransicionEstadoSesionInvalidaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "TRANSICION_INVALIDA", ex.Message);
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
