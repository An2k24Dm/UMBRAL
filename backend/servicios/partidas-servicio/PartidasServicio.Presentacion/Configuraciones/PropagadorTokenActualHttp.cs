using Microsoft.Net.Http.Headers;
using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Presentacion.Configuraciones;

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
