namespace SesionesServicio.Dominio.Excepciones;

public sealed class SesionNoEncontradaExcepcion : Exception
{
    public SesionNoEncontradaExcepcion(string mensaje) : base(mensaje) { }
}
