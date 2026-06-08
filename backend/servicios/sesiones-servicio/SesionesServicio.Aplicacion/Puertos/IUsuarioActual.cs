namespace SesionesServicio.Aplicacion.Puertos;

// Sesiones-servicio NO consulta la base de datos de identidad. Toda la
// información del usuario actual proviene del JWT emitido por Keycloak,
// que el gateway propaga al microservicio. Esta abstracción aísla a la
// capa de Aplicación de HttpContext y permite probarla con dobles.
public interface IUsuarioActual
{
    bool EstaAutenticado { get; }
    Guid? Id { get; }
    string? IdKeycloak { get; }
    string? NombreUsuario { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool TieneAlgunRol(params string[] roles);
}
