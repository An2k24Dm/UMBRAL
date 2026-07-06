namespace PartidasServicio.Dominio.Excepciones;

public sealed class PreguntaNoEncontradaExcepcion : ExcepcionDominio
{
    public PreguntaNoEncontradaExcepcion(Guid preguntaId)
        : base($"La pregunta con ID '{preguntaId}' no fue encontrada en la trivia de esta etapa.") { }
}
