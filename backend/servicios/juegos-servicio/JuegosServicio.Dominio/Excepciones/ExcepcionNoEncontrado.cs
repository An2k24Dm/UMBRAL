namespace JuegosServicio.Dominio.Excepciones;

public sealed class ExcepcionNoEncontrado : Exception
{
    public ExcepcionNoEncontrado(string mensaje) : base(mensaje) { }
}
