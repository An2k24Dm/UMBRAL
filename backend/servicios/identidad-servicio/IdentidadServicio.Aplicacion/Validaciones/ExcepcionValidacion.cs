namespace IdentidadServicio.Aplicacion.Validaciones;

// Vive en la capa de aplicación porque representa errores de validación de
// casos de uso/DTO antes de llegar al dominio. El middleware la mapea a HTTP 400
// con un payload { mensaje, errores: [{ campo, mensaje }] }.
public sealed class ExcepcionValidacion : Exception
{
    public IReadOnlyList<ErrorValidacion> Errores { get; }

    public ExcepcionValidacion(string mensaje, IEnumerable<ErrorValidacion> errores)
        : base(mensaje)
    {
        Errores = errores.ToList();
    }
}
