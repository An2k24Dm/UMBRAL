namespace JuegosServicio.Dominio.Eventos;

public sealed class TriviaCreadaEvento : EventoDominio
{
    public Guid TriviaId { get; }
    public string Nombre { get; }

    public TriviaCreadaEvento(Guid triviaId, string nombre)
    {
        TriviaId = triviaId;
        Nombre = nombre;
    }
}
