namespace SesionesServicio.Dominio.Excepciones;

public sealed class ContenidoJuegoNoEncontradoExcepcion : Exception
{
    public ContenidoJuegoNoEncontradoExcepcion(string mensaje) : base(mensaje) { }
}
