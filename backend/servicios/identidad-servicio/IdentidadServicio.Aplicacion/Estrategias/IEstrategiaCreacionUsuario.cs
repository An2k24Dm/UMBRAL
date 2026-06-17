using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Estrategias;

public interface IEstrategiaCreacionUsuario
{
    bool PuedeCrear(RolUsuario rol);
    RolUsuario ObtenerRol();
    Task<Usuario> CrearUsuarioDominioAsync(
        DatosCreacionUsuario datos,
        DateTime fechaRegistro,
        CancellationToken cancelacion);
}
