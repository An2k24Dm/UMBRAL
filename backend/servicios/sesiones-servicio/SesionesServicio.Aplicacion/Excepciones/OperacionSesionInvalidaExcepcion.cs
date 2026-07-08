namespace SesionesServicio.Aplicacion.Excepciones;

public sealed class OperacionSesionInvalidaExcepcion : Exception
{
    public OperacionSesionInvalidaExcepcion(string mensaje) : base(mensaje) { }
}
