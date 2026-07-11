using System.Net;
using System.Text.Json;
using SesionesServicio.Presentacion.Configuraciones;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Presentacion.Middlewares;

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
        catch (ParticipanteYaEstaEnSesionActivaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "PARTICIPANTE_EN_SESION_ACTIVA", ex.Message);
        }
        catch (ParticipanteYaPerteneceASesionExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "PARTICIPANTE_YA_INSCRITO", ex.Message);
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
        catch (ParticipanteNoEncontradoExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.NotFound,
                "PARTICIPANTE_NO_ENCONTRADO", ex.Message);
        }
        catch (ExpulsionNoPermitidaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "EXPULSION_NO_PERMITIDA", ex.Message);
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
        catch (OperacionSesionInvalidaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                "OPERACION_SESION_INVALIDA", ex.Message);
        }
        catch (RespuestaTriviaDuplicadaExcepcion ex)
        {
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                ex.EsEquipo ? "EQUIPO_YA_RESPONDIO" : "YA_RESPONDIDA", ex.Message);
        }
        catch (EvidenciaTesoroDuplicadaExcepcion ex)
        {
            // Individual: el participante ya completó la etapa. Grupal: otro
            // integrante del equipo encontró el tesoro primero. Conflicto de
            // negocio (409), nunca un 500.
            await EscribirCodigoAsync(contexto, HttpStatusCode.Conflict,
                ex.EsEquipo ? "EQUIPO_YA_COMPLETO_ETAPA" : "PARTICIPANTE_YA_COMPLETO_ETAPA",
                ex.Message);
        }
        catch (JsonException ex)
        {
            var correlationId = ObtenerCorrelationId(contexto);
            _registro.LogWarning(
                ex,
                "Solicitud rechazada por JSON inválido. CorrelationId={CorrelationId}",
                correlationId);
            await EscribirJsonAsync(contexto, HttpStatusCode.BadRequest,
                RespuestaErrorModelo.ConstruirDesdeJsonException(ex, correlationId));
        }
        catch (BadHttpRequestException ex) when (ex.InnerException is JsonException jsonInner)
        {
            var correlationId = ObtenerCorrelationId(contexto);
            _registro.LogWarning(
                ex,
                "Solicitud rechazada por JSON inválido. CorrelationId={CorrelationId}",
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

    private static string? ObtenerCorrelationId(HttpContext contexto)
        => contexto.Items.TryGetValue("CorrelationId", out var valor) ? valor as string : null;

    private static Task EscribirJsonAsync(HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
