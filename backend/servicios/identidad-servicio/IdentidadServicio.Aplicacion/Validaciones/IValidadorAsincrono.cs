namespace IdentidadServicio.Aplicacion.Validaciones;

// Contrato genérico para validadores que SÍ necesitan acceso a recursos
// asincrónicos (típicamente repositorios).
//
// Coexiste con IValidador<T> (sincrónico) para mantener una separación clara:
//  * IValidador<T>           — validaciones puras de formato / rangos.
//  * IValidadorAsincrono<T>  — validaciones que dependen de I/O (duplicados,
//                              estado externo, etc.).
public interface IValidadorAsincrono<in TSolicitud>
{
    Task<ResultadoValidacion> ValidarAsync(
        TSolicitud solicitud,
        CancellationToken cancelacion);
}
