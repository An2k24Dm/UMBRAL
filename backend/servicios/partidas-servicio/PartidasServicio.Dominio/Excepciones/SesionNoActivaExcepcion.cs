namespace PartidasServicio.Dominio.Excepciones;

public sealed class SesionNoActivaExcepcion : ExcepcionDominio
{
    public SesionNoActivaExcepcion(string estado)
        : base($"La sesión no está activa. Estado actual: {estado}.") { }
}
