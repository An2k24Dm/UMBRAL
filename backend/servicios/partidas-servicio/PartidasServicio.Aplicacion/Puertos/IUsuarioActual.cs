namespace PartidasServicio.Aplicacion.Puertos;

public interface IUsuarioActual
{
    bool EstaAutenticado();
    Guid? ObtenerId();
    string? ObtenerNombreUsuario();
    IReadOnlyCollection<string> ObtenerRoles();
}
