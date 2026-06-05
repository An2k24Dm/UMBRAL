using System.Security.Claims;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Presentacion.Configuraciones;

public sealed class UsuarioActualHttp : IUsuarioActual
{
    private readonly IHttpContextAccessor _accesor;

    public UsuarioActualHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    public string? IdKeycloak
    {
        get
        {
            var usuario = _accesor.HttpContext?.User;
            if (usuario?.Identity?.IsAuthenticated != true) return null;
            return usuario.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? usuario.FindFirstValue("sub");
        }
    }

    public bool EstaAutenticado
        => _accesor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}
