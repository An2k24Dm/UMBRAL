using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

// Strategy: una implementación por cada TipoUsuario.
//   - PuedeCrear: criterio de selección (lo usa la fábrica).
//   - ObtenerRol: rol que se asignará en Keycloak.
//   - CrearUsuarioDominio: factory de la entidad concreta.
//   - GuardarAsync: delega en el método específico del repositorio.
public interface IEstrategiaCreacionUsuario
{
    bool PuedeCrear(TipoUsuario tipoUsuario);
    RolUsuario ObtenerRol();
    Usuario CrearUsuarioDominio(CrearUsuarioDto dto, DateTime fechaRegistro);
    Task GuardarAsync(
        Usuario usuario,
        string idKeycloak,
        IRepositorioIdentidad repositorio,
        CancellationToken cancelacion);
}
