namespace JuegosServicio.Dominio.Eventos;

public sealed class TriviaArchivadaEvento : EventoDominio
{
    public Guid TriviaId { get; }

    public TriviaArchivadaEvento(Guid triviaId)
    {
        TriviaId = triviaId;
    }
}
