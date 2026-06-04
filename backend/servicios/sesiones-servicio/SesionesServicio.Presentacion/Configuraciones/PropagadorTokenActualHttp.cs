using Microsoft.Net.Http.Headers;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Presentacion.Configuraciones;

// Extrae el token Bearer del header Authorization del request actual
// para que el cliente HTTP hacia juegos-servicio pueda reenviarlo.
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
