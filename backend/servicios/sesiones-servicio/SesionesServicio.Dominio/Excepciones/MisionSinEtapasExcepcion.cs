namespace SesionesServicio.Dominio.Excepciones;

public sealed class MisionSinEtapasExcepcion : Exception
{
    public MisionSinEtapasExcepcion(string mensaje) : base(mensaje) { }
}
