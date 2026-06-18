using IdentidadServicio.Aplicacion.Validaciones;

namespace IdentidadServicio.Aplicacion.Puertos;

// Encapsula la regla de aplicación: un usuario autenticado no puede usar el
// sistema si su cuenta está inactiva. No depende de HTTP.
public interface IValidadorAccesoUsuarioActivo
{
    Task<ResultadoAccesoUsuarioActivo> ValidarAsync(
        string idKeycloak,
        CancellationToken cancelacion);
}
