namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class ExcepcionValidacion : Exception
{
    public IReadOnlyList<ErrorValidacion> Errores { get; }

    public ExcepcionValidacion(string mensaje, IEnumerable<ErrorValidacion> errores)
        : base(mensaje)
    {
        Errores = errores.ToList();
    }
}
