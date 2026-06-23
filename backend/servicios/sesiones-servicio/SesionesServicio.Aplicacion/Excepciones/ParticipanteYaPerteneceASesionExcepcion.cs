namespace SesionesServicio.Aplicacion.Excepciones;

public sealed class ParticipanteYaPerteneceASesionExcepcion : Exception
{
    public ParticipanteYaPerteneceASesionExcepcion(string mensaje) : base(mensaje) { }
}
