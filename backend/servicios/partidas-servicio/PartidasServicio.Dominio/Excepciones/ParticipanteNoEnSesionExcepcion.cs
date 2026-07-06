namespace PartidasServicio.Dominio.Excepciones;

public sealed class ParticipanteNoEnSesionExcepcion : ExcepcionDominio
{
    public ParticipanteNoEnSesionExcepcion()
        : base("El participante no está inscrito en esta sesión.") { }
}
