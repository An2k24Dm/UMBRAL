namespace SesionesServicio.Aplicacion.Excepciones;

public sealed class ParticipanteYaEstaEnSesionActivaExcepcion : Exception
{
    public ParticipanteYaEstaEnSesionActivaExcepcion(string mensaje) : base(mensaje) { }
}
