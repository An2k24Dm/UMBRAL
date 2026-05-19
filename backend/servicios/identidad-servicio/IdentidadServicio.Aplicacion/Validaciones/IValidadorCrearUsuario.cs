using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public interface IValidadorCrearUsuario
{
    // Valida el DTO completo. Si tiene errores, lanza ExcepcionValidacion.
    // Normaliza in-situ el teléfono (sin espacios ni guiones) para que el
    // dominio y la persistencia trabajen con la forma canónica.
    Task ValidarAsync(CrearUsuarioDto dto, CancellationToken cancelacion);
}
