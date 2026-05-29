namespace SesionesServicio.Dominio.Excepciones;

public sealed class SesionInvalidaExcepcion : Exception
{
    public SesionInvalidaExcepcion(string mensaje) : base(mensaje) { }
}
