using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

// Strategy: una implementación por cada RolUsuario.
//   - PuedeCrear: criterio de selección (lo usa la fábrica).
//   - ObtenerRol: rol que se asignará en Keycloak.
//   - CrearUsuarioDominioAsync: factory de la entidad concreta. Recibe el
//     modelo interno DatosCreacionUsuario para que las estrategias no dependan
//     de los DTOs de transporte. Es async porque Operador/Administrador llaman
//     al GeneradorCodigoUsuario.
//   - GuardarAsync: delega en el método específico del repositorio.
public interface IEstrategiaCreacionUsuario
{
    bool PuedeCrear(RolUsuario rol);
    RolUsuario ObtenerRol();
    Task<Usuario> CrearUsuarioDominioAsync(
        DatosCreacionUsuario datos,
        DateTime fechaRegistro,
        CancellationToken cancelacion);
    Task GuardarAsync(
        Usuario usuario,
        string idKeycloak,
        IRepositorioIdentidad repositorio,
        CancellationToken cancelacion);
}
