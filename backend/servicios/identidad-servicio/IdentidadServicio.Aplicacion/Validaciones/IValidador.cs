namespace IdentidadServicio.Aplicacion.Validaciones;

// Contrato genérico de validación reutilizable por cada caso de uso.
//
// La interfaz es **sincrónica** a propósito: aquí se cubre la validación de
// entrada del caso de uso (formato, obligatoriedad, rangos, reglas cruzadas
// sobre los datos del comando). Cualquier comprobación que necesite acceso a
// base de datos — por ejemplo duplicados — debe ejecutarse en el manejador
// del caso de uso, donde sí está inyectado el repositorio. Mantener este
// límite evita acoplar la capa de validación con la persistencia.
//
// Cada validador específico se especializa en un comando (tipado fuerte) y
// devuelve un ResultadoValidacion que el manejador puede inspeccionar o, lo
// más habitual, propagar como ExcepcionValidacion con LanzarSiHayErrores.
public interface IValidador<in TSolicitud>
{
    ResultadoValidacion Validar(TSolicitud solicitud);
}
