namespace IdentidadServicio.Aplicacion.Puertos;

public interface IUsuarioActual
{
    string? IdKeycloak { get; }
    bool EstaAutenticado { get; }
}
