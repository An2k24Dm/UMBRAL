namespace SesionesServicio.Dominio.Excepciones;

public sealed class MisionNoActivaExcepcion : Exception
{
    public MisionNoActivaExcepcion(string mensaje) : base(mensaje) { }
}
