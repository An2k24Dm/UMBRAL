using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

// Puerto específico para Operadores. Lo usan HU02 (registro), HU09 (edición),
// y el generador de códigos OP-### (que necesita conocer el último código
// asignado).
//
// Persistencia atomicidad: Agregar/Actualizar dejan los cambios en el contexto
// de EF (tracked) pero NO ejecutan SaveChanges. La confirmación final la hace
// IUnidadTrabajoIdentidad.GuardarCambiosAsync en el manejador. Esto permite
// que el caso de uso decida cuándo "cerrar" la transacción y, si necesita,
// compensar tras un error externo (p. ej. Keycloak).
public interface IRepositorioOperadores
{
    // HU09 — recupera el agregado Operador completo (Usuario + Persona +
    // Operador). Devuelve null si el id no existe o no es Operador.
    Task<Operador?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);

    // HU02 — alta. El idKeycloak vive solo en infraestructura: el dominio no
    // lo conoce. Solo deja los modelos preparados; la confirmación va por
    // IUnidadTrabajoIdentidad.
    Task AgregarAsync(
        Operador operador, string idKeycloak, CancellationToken cancelacion);

    // HU09 — edición parcial. Copia los campos editables del agregado en los
    // modelos persistidos sin tocar Estado, FechaRegistro ni Rol. Devuelve el
    // IdKeycloak del Operador para que el manejador pueda propagar el cambio
    // a Keycloak tras GuardarCambios.
    Task<string> ActualizarAsync(Operador operador, CancellationToken cancelacion);

    // Generador HU02 — devuelve el último código OP-### (orden alfabético
    // descendente, equivalente a numérico hasta 999). Null si no hay ninguno.
    Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion);
}
