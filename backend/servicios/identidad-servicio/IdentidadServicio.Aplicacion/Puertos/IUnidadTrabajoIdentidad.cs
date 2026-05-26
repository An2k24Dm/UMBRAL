namespace IdentidadServicio.Aplicacion.Puertos;

// Puerto Unidad de Trabajo. Los repositorios concretos solo dejan los cambios
// preparados en el contexto de persistencia (Add/Update sobre los modelos);
// la confirmación atómica la dispara el manejador llamando a este puerto al
// final del caso de uso.
//
// El control explícito permite que el manejador decida cuándo "cerrar" la
// transacción y, si algo externo (p. ej. Keycloak) falla después, no quede
// la base en estado inconsistente.
public interface IUnidadTrabajoIdentidad
{
    Task GuardarCambiosAsync(CancellationToken cancelacion);
}
