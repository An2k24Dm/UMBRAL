using JuegosServicio.Aplicacion.Puertos;
using Microsoft.Net.Http.Headers;

namespace JuegosServicio.Api.Configuraciones;

// Extrae el token Bearer del header Authorization del request actual
// para que el cliente HTTP hacia sesiones-servicio pueda reenviarlo.
// Vive en la capa API porque depende de HttpContext; Aplicación e
// Infraestructura sólo conocen la interfaz IPropagadorTokenActual.
public sealed class PropagadorTokenActualHttp : IPropagadorTokenActual
{
    private const string Prefijo = "Bearer ";
    private readonly IHttpContextAccessor _accesor;

    public PropagadorTokenActualHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    public string? ObtenerTokenActual()
    {
        var header = _accesor.HttpContext?.Request?.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrWhiteSpace(header)) return null;
        return header.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase)
            ? header[Prefijo.Length..].Trim()
            : null;
    }
}
