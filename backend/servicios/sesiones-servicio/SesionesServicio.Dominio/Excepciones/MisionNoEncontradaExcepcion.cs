namespace SesionesServicio.Dominio.Excepciones;

public sealed class MisionNoEncontradaExcepcion : Exception
{
    public MisionNoEncontradaExcepcion(string mensaje) : base(mensaje) { }
}
