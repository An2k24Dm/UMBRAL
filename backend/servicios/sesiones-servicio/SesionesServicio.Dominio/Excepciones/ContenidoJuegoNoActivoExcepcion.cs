namespace SesionesServicio.Dominio.Excepciones;

public sealed class ContenidoJuegoNoActivoExcepcion : Exception
{
    public ContenidoJuegoNoActivoExcepcion(string mensaje) : base(mensaje) { }
}
