namespace PartidasServicio.Dominio.Excepciones;

public sealed class PartidaNoEncontradaExcepcion : ExcepcionDominio
{
    public PartidaNoEncontradaExcepcion(Guid sesionId)
        : base($"No existe una partida para la sesión '{sesionId}'.") { }
}
