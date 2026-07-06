namespace PartidasServicio.Aplicacion.Validaciones;

public sealed class ExcepcionValidacion : Exception
{
    public IReadOnlyList<ErrorValidacion> Errores { get; }

    public ExcepcionValidacion(string mensaje, IReadOnlyList<ErrorValidacion> errores)
        : base(mensaje)
    {
        Errores = errores;
    }
}
