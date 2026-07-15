namespace SesionesServicio.Dominio.Excepciones;

// Se lanza al intentar modificar una sesión que no está en estado Programada.
public sealed class SesionNoModificableExcepcion : Exception
{
    public SesionNoModificableExcepcion(string mensaje) : base(mensaje) { }
}
