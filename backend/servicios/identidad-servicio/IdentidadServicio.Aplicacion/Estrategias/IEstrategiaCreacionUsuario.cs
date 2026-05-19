using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

// Strategy: una implementación por cada TipoUsuario.
//   - PuedeCrear: criterio de selección (lo usa la fábrica).
//   - ObtenerRol: rol que se asignará en Keycloak.
//   - CrearUsuarioDominioAsync: factory de la entidad concreta. Es async porque
//     las estrategias de Operador y Administrador llaman al GeneradorCodigoUsuario.
//   - GuardarAsync: delega en el método específico del repositorio.
public interface IEstrategiaCreacionUsuario
{
    bool PuedeCrear(TipoUsuario tipoUsuario);
    RolUsuario ObtenerRol();
    Task<Usuario> CrearUsuarioDominioAsync(
        CrearUsuarioDto dto,
        DateTime fechaRegistro,
        CancellationToken cancelacion);
    Task GuardarAsync(
        Usuario usuario,
        string idKeycloak,
        IRepositorioIdentidad repositorio,
        CancellationToken cancelacion);
}
