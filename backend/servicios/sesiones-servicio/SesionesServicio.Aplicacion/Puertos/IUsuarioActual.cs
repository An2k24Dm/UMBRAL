namespace SesionesServicio.Aplicacion.Puertos;

// Puerto de aplicación: representa al usuario autenticado del request actual,
// obtenido desde JWT/HTTP/claims. No es una abstracción del dominio. Expone
// métodos (no propiedades) siguiendo la convención del proyecto.
public interface IUsuarioActual
{
    bool EstaAutenticado();
    Guid? ObtenerId();
    string? ObtenerIdKeycloak();
    string? ObtenerNombreUsuario();
    IReadOnlyCollection<string> ObtenerRoles();
    bool TieneAlgunRol(params string[] roles);
}
