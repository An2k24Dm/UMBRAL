namespace SesionesServicio.Dominio.Excepciones;

public sealed class ParticipanteNoEncontradoExcepcion : Exception
{
    public ParticipanteNoEncontradoExcepcion(string mensaje) : base(mensaje) { }
}
