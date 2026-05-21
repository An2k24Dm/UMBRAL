namespace IdentidadServicio.Aplicacion.Validaciones;

// Contrato genérico de validación reutilizable por cada caso de uso. Cada
// validador concreto se especializa en un DTO específico (tipado fuerte) y
// lanza ExcepcionValidacion cuando los datos no cumplen las reglas.
//
// Los validadores normalizan in-situ los campos cuando aplica (p. ej. el
// teléfono pierde espacios y guiones) para que el dominio y la persistencia
// reciban siempre la forma canónica.
public interface IValidador<TDatos>
{
    Task ValidarAsync(TDatos datos, CancellationToken cancelacion);
}
