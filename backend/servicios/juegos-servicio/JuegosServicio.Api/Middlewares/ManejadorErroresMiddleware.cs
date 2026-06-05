using System.Net;
using System.Text.Json;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Api.Middlewares;

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
        catch (Exception ex)
        {
            _registro.LogError(ex, "Error no controlado en juegos-servicio.");
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
