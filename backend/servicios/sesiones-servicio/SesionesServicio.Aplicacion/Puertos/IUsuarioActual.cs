namespace SesionesServicio.Aplicacion.Puertos;

public interface IUsuarioActual
{
    bool EstaAutenticado();
    Guid? ObtenerId();
    string? ObtenerIdKeycloak();
    string? ObtenerNombreUsuario();
    IReadOnlyCollection<string> ObtenerRoles();
    bool TieneAlgunRol(params string[] roles);
}
