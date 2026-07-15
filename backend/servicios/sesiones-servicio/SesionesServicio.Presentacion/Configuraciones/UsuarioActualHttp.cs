using System.Security.Claims;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Presentacion.Configuraciones;

// Adaptador HTTP de IUsuarioActual: lee los claims del JWT vigente
// (aplanados a "roles" por RegistroSeguridad). El Id en formato Guid
// se obtiene del sub cuando es válido; Keycloak ocasionalmente emite
// subs no UUID en cuentas administrativas, en cuyo caso devolvemos
// null y dejamos que el agregado guarde Guid.Empty como creador.
public sealed class UsuarioActualHttp : IUsuarioActual
{
    private readonly IHttpContextAccessor _accesor;

    public UsuarioActualHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    public bool EstaAutenticado()
        => _accesor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? ObtenerId()
    {
        var sub = ObtenerIdKeycloak();
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public string? ObtenerIdKeycloak()
    {
        var usuario = _accesor.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true) return null;
        return usuario.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? usuario.FindFirstValue("sub");
    }

    public string? ObtenerNombreUsuario()
    {
        var usuario = _accesor.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true) return null;
        return usuario.Identity.Name
               ?? usuario.FindFirstValue("preferred_username");
    }

    public IReadOnlyCollection<string> ObtenerRoles()
    {
        var usuario = _accesor.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true) return Array.Empty<string>();
        return usuario.FindAll("roles")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    public bool TieneAlgunRol(params string[] roles)
    {
        if (roles is null || roles.Length == 0) return false;
        var actuales = ObtenerRoles();
        return roles.Any(r => actuales.Contains(r));
    }
}
