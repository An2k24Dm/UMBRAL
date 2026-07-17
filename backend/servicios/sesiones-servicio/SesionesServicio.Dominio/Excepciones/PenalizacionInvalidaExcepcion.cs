namespace SesionesServicio.Dominio.Excepciones;

public sealed class PenalizacionInvalidaExcepcion : Exception
{
    public PenalizacionInvalidaExcepcion(string mensaje) : base(mensaje) { }
}
