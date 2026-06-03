namespace SesionesServicio.Dominio.Excepciones;

public sealed class ParticipacionInvalidaExcepcion : Exception
{
    public ParticipacionInvalidaExcepcion(string mensaje) : base(mensaje) { }
}
