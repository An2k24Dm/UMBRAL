namespace SesionesServicio.Dominio.Excepciones;

public sealed class ExpulsionNoPermitidaExcepcion : Exception
{
    public ExpulsionNoPermitidaExcepcion(string mensaje) : base(mensaje) { }
}
