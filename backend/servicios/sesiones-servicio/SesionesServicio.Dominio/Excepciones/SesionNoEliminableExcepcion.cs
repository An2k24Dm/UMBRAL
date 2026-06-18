namespace SesionesServicio.Dominio.Excepciones;

// Se lanza al intentar eliminar una sesión que no está en estado Programada.
public sealed class SesionNoEliminableExcepcion : Exception
{
    public SesionNoEliminableExcepcion(string mensaje) : base(mensaje) { }
}
